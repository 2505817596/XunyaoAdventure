using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class MailStore
{
    private readonly System.Threading.Lock _gate = new();
    private readonly GameDatabase _database;
    private readonly GameAccountStore _accounts;
    private readonly List<MailMessage> _mails = new();
    private long _nextMailId = 1;

    public event Action? MailsChanged;

    public MailStore(GameDatabase database, GameAccountStore accounts)
    {
        _database = database;
        _accounts = accounts;
        LoadMails();
    }

    public IReadOnlyList<MailMessage> GetInbox(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Array.Empty<MailMessage>();
        }

        string normalized = userName.Trim();
        lock (_gate)
        {
            return _mails
                .Where(mail => string.Equals(mail.UserName, normalized, StringComparison.OrdinalIgnoreCase))
                .OrderBy(mail => mail.Claimed)
                .ThenBy(mail => mail.Read)
                .ThenByDescending(mail => mail.SentAt)
                .Select(CloneMail)
                .ToList();
        }
    }

    public int GetUnreadCount(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return 0;
        }

        string normalized = userName.Trim();
        lock (_gate)
        {
            return _mails.Count(mail =>
                string.Equals(mail.UserName, normalized, StringComparison.OrdinalIgnoreCase)
                && (!mail.Read || (mail.Attachments.Count > 0 && !mail.Claimed)));
        }
    }

    public MailMessage? GetMail(string? userName, long id)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        string normalized = userName.Trim();
        lock (_gate)
        {
            MailMessage? mail = _mails.FirstOrDefault(item =>
                item.Id == id && string.Equals(item.UserName, normalized, StringComparison.OrdinalIgnoreCase));
            return mail is null ? null : CloneMail(mail);
        }
    }

    public MailMessage SendMail(
        string userName,
        string title,
        string content,
        IReadOnlyList<QuestReward> attachments,
        string senderName = "系统")
    {
        MailMessage mail;
        lock (_gate)
        {
            mail = CreateMail(userName, title, content, attachments, senderName, DateTimeOffset.UtcNow);
        }

        MailsChanged?.Invoke();
        return CloneMail(mail);
    }

    public IReadOnlyList<MailMessage> SendMailToAll(
        string title,
        string content,
        IReadOnlyList<QuestReward> attachments,
        string senderName = "系统")
    {
        IReadOnlyList<PlayerAccount> accounts = _accounts.GetAccounts();
        List<MailMessage> mails = new();
        DateTimeOffset sentAt = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            foreach (PlayerAccount account in accounts)
            {
                if (string.IsNullOrWhiteSpace(account.UserName))
                {
                    continue;
                }

                mails.Add(CreateMail(account.UserName, title, content, attachments, senderName, sentAt));
            }
        }

        if (mails.Count > 0)
        {
            MailsChanged?.Invoke();
        }

        return mails.Select(CloneMail).ToList();
    }

    public MailActionResult MarkRead(string? userName, long id)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new MailActionResult(false, "请先登录", null, null);
        }

        lock (_gate)
        {
            MailMessage? mail = FindOwnedMail(userName, id);
            if (mail is null)
            {
                return new MailActionResult(false, "邮件不存在", null, _accounts.GetAccount(userName));
            }

            if (!mail.Read)
            {
                mail.Read = true;
                SaveMail(mail);
            }

            return new MailActionResult(true, "已读", CloneMail(mail), _accounts.GetAccount(userName));
        }
    }

    public MailActionResult ClaimAttachments(string? userName, long id)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new MailActionResult(false, "请先登录", null, null);
        }

        MailMessage? clonedMail;
        PlayerAccount? account;
        lock (_gate)
        {
            MailMessage? mail = FindOwnedMail(userName, id);
            if (mail is null)
            {
                return new MailActionResult(false, "邮件不存在", null, _accounts.GetAccount(userName));
            }

            if (mail.Attachments.Count == 0)
            {
                mail.Read = true;
                SaveMail(mail);
                return new MailActionResult(false, "这封邮件没有附件", CloneMail(mail), _accounts.GetAccount(userName));
            }

            if (mail.Claimed)
            {
                return new MailActionResult(false, "附件已经领取", CloneMail(mail), _accounts.GetAccount(userName));
            }

            account = _accounts.GrantRewards(userName, mail.Attachments);
            if (account is null)
            {
                return new MailActionResult(false, "账号不存在", CloneMail(mail), null);
            }

            mail.Read = true;
            mail.Claimed = true;
            SaveMail(mail);
            clonedMail = CloneMail(mail);
        }

        MailsChanged?.Invoke();
        return new MailActionResult(true, "附件已领取", clonedMail, account);
    }

    public MailClaimAllResult ClaimAllAttachments(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new MailClaimAllResult(false, "请先登录", Array.Empty<MailMessage>(), null, 0);
        }

        string normalized = userName.Trim();
        List<MailMessage> inbox;
        List<MailMessage> claimable;
        PlayerAccount? account;
        lock (_gate)
        {
            inbox = _mails
                .Where(mail => string.Equals(mail.UserName, normalized, StringComparison.OrdinalIgnoreCase))
                .ToList();
            claimable = inbox
                .Where(mail => mail.Attachments.Count > 0 && !mail.Claimed)
                .ToList();

            if (claimable.Count == 0)
            {
                return new MailClaimAllResult(
                    false,
                    "没有可领取附件",
                    SortInbox(inbox).Select(CloneMail).ToList(),
                    _accounts.GetAccount(userName),
                    0);
            }

            List<QuestReward> rewards = claimable
                .SelectMany(mail => mail.Attachments)
                .Select(CloneReward)
                .ToList();
            account = _accounts.GrantRewards(userName, rewards);
            if (account is null)
            {
                return new MailClaimAllResult(
                    false,
                    "账号不存在",
                    SortInbox(inbox).Select(CloneMail).ToList(),
                    null,
                    0);
            }

            foreach (MailMessage mail in claimable)
            {
                mail.Read = true;
                mail.Claimed = true;
                SaveMail(mail);
            }
        }

        MailsChanged?.Invoke();
        IReadOnlyList<MailMessage> mails = GetInbox(userName);
        return new MailClaimAllResult(true, $"已领取{claimable.Count}封邮件附件", mails, account, claimable.Count);
    }

    public MailActionResult DeleteMail(string? userName, long id)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new MailActionResult(false, "请先登录", null, null);
        }

        PlayerAccount? account;
        lock (_gate)
        {
            MailMessage? mail = FindOwnedMail(userName, id);
            account = _accounts.GetAccount(userName);
            if (mail is null)
            {
                return new MailActionResult(false, "邮件不存在", null, account);
            }

            if (mail.Attachments.Count > 0 && !mail.Claimed)
            {
                return new MailActionResult(false, "请先领取附件", CloneMail(mail), account);
            }

            _mails.Remove(mail);
            DeleteMailDocument(mail.Id);
        }

        MailsChanged?.Invoke();
        return new MailActionResult(true, "邮件已删除", null, account);
    }

    private MailMessage? FindOwnedMail(string userName, long id)
        => _mails.FirstOrDefault(mail =>
            mail.Id == id && string.Equals(mail.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase));

    private MailMessage CreateMail(
        string userName,
        string title,
        string content,
        IReadOnlyList<QuestReward> attachments,
        string senderName,
        DateTimeOffset sentAt)
    {
        MailMessage mail = new(
            _nextMailId++,
            userName.Trim(),
            NormalizeText(title, "系统邮件"),
            NormalizeText(content, string.Empty),
            NormalizeText(senderName, "系统"),
            sentAt,
            false,
            false,
            attachments.Where(reward => reward.Quantity > 0).Select(CloneReward).ToList());

        _mails.Add(mail);
        SaveMail(mail);
        return mail;
    }

    private static IEnumerable<MailMessage> SortInbox(IEnumerable<MailMessage> mails)
        => mails
            .OrderBy(mail => mail.Claimed)
            .ThenBy(mail => mail.Read)
            .ThenByDescending(mail => mail.SentAt);

    private void LoadMails()
    {
        ILiteCollection<MailMessageDocument> collection = _database.GetCollection<MailMessageDocument>("mail_messages");
        collection.EnsureIndex(mail => mail.Id, unique: true);
        collection.EnsureIndex(mail => mail.UserName);

        foreach (MailMessageDocument document in collection.FindAll().OrderBy(mail => mail.Id))
        {
            _mails.Add(ToMail(document));
        }

        _nextMailId = _mails.Select(mail => mail.Id).DefaultIfEmpty(0).Max() + 1;
    }

    private void SaveMail(MailMessage mail)
    {
        ILiteCollection<MailMessageDocument> collection = _database.GetCollection<MailMessageDocument>("mail_messages");
        collection.Upsert(ToDocument(mail));
    }

    private void DeleteMailDocument(long id)
    {
        ILiteCollection<MailMessageDocument> collection = _database.GetCollection<MailMessageDocument>("mail_messages");
        collection.Delete(id);
    }

    private static MailMessage CloneMail(MailMessage mail)
        => new(
            mail.Id,
            mail.UserName,
            mail.Title,
            mail.Content,
            mail.SenderName,
            mail.SentAt,
            mail.Read,
            mail.Claimed,
            mail.Attachments.Select(CloneReward).ToList());

    private static QuestReward CloneReward(QuestReward reward)
        => new(reward.Type, reward.Name, reward.Quantity, reward.TemplateId);

    private static MailMessage ToMail(MailMessageDocument document)
        => new(
            document.Id,
            document.UserName,
            document.Title,
            document.Content,
            document.SenderName,
            document.SentAt,
            document.Read,
            document.Claimed,
            document.Attachments.Select(ToReward).ToList());

    private static MailMessageDocument ToDocument(MailMessage mail)
        => new()
        {
            Id = mail.Id,
            UserName = mail.UserName,
            Title = mail.Title,
            Content = mail.Content,
            SenderName = mail.SenderName,
            SentAt = mail.SentAt,
            Read = mail.Read,
            Claimed = mail.Claimed,
            Attachments = mail.Attachments.Select(ToRewardDocument).ToList(),
        };

    private static QuestReward ToReward(OperationRewardItemDocument document)
        => new(
            Enum.TryParse(document.Type, ignoreCase: true, out QuestRewardType type) ? type : QuestRewardType.Copper,
            document.Name,
            Math.Max(1, document.Quantity),
            string.IsNullOrWhiteSpace(document.TemplateId) ? null : document.TemplateId);

    private static OperationRewardItemDocument ToRewardDocument(QuestReward reward)
        => new()
        {
            Type = reward.Type.ToString(),
            Name = reward.Name,
            TemplateId = reward.TemplateId ?? string.Empty,
            Quantity = reward.Quantity,
        };

    private static string NormalizeText(string? value, string fallback)
    {
        string normalized = string.Join(
            ' ',
            (value ?? string.Empty)
                .Trim()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
