using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class ChatStore
{
    public const int MaxMessageLength = 80;
    public const int MaxStoredMessages = 120;

    private static readonly TimeSpan SendCooldown = TimeSpan.FromMilliseconds(800);

    private readonly object _gate = new();
    private readonly GameDatabase _database;
    private readonly GameAccountStore _accounts;
    private readonly FriendStore _friends;
    private readonly List<ChatMessage> _messages = new();
    private readonly Dictionary<string, long> _directReadStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTimeOffset> _lastSentAt = new(StringComparer.OrdinalIgnoreCase);
    private long _nextMessageId = 1;

    public event Action? MessagesChanged;

    public ChatStore(GameDatabase database, GameAccountStore accounts, FriendStore friends)
    {
        _database = database;
        _accounts = accounts;
        _friends = friends;
        LoadMessages();
        LoadDirectReadStates();

        if (!_messages.Any(message => message.Channel == ChatChannel.System))
        {
            AddSystemMessage("欢迎来到寻妖录。");
        }
    }

    public IReadOnlyList<ChatMessage> GetMessages(ChatChannel channel, int? guildId = null)
    {
        lock (_gate)
        {
            return _messages
                .Where(message => message.Channel == channel)
                .Where(message => channel != ChatChannel.Guild || message.GuildId == guildId)
                .Where(message => channel != ChatChannel.Direct)
                .OrderBy(message => message.Id)
                .ToList();
        }
    }

    public IReadOnlyList<ChatMessage> GetDirectMessages(string? userName, string? friendUserName)
    {
        string normalizedUser = NormalizeUserName(userName);
        string normalizedFriend = NormalizeUserName(friendUserName);
        if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(normalizedFriend))
        {
            return Array.Empty<ChatMessage>();
        }

        lock (_gate)
        {
            return _messages
                .Where(message => message.Channel == ChatChannel.Direct)
                .Where(message =>
                    (SameUser(message.SenderUserName, normalizedUser) && SameUser(message.RecipientUserName, normalizedFriend))
                    || (SameUser(message.SenderUserName, normalizedFriend) && SameUser(message.RecipientUserName, normalizedUser)))
                .OrderBy(message => message.Id)
                .ToList();
        }
    }

    public DirectChatSummary GetDirectChatSummary(string? userName, string? friendUserName)
    {
        string normalizedUser = NormalizeUserName(userName);
        string normalizedFriend = NormalizeUserName(friendUserName);
        if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(normalizedFriend))
        {
            return new DirectChatSummary(normalizedFriend, null, 0);
        }

        lock (_gate)
        {
            ChatMessage? latestMessage = GetDirectMessagesCore(normalizedUser, normalizedFriend)
                .OrderByDescending(message => message.Id)
                .FirstOrDefault();
            long lastReadMessageId = GetLastReadMessageIdCore(normalizedUser, normalizedFriend);
            int unreadCount = GetUnreadDirectCountCore(normalizedUser, normalizedFriend, lastReadMessageId);
            return new DirectChatSummary(normalizedFriend, latestMessage, unreadCount);
        }
    }

    public int GetUnreadDirectCount(string? userName)
    {
        string normalizedUser = NormalizeUserName(userName);
        if (string.IsNullOrWhiteSpace(normalizedUser))
        {
            return 0;
        }

        lock (_gate)
        {
            return _messages
                .Where(message => message.Channel == ChatChannel.Direct)
                .Where(message => SameUser(message.RecipientUserName, normalizedUser))
                .Count(message => message.Id > GetLastReadMessageIdCore(normalizedUser, message.SenderUserName));
        }
    }

    public void MarkDirectChatRead(string? userName, string? friendUserName)
    {
        string normalizedUser = NormalizeUserName(userName);
        string normalizedFriend = NormalizeUserName(friendUserName);
        if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(normalizedFriend))
        {
            return;
        }

        bool changed = false;
        lock (_gate)
        {
            long latestMessageId = GetDirectMessagesCore(normalizedUser, normalizedFriend)
                .Select(message => message.Id)
                .DefaultIfEmpty(0)
                .Max();
            if (latestMessageId <= 0)
            {
                return;
            }

            string key = BuildDirectReadStateId(normalizedUser, normalizedFriend);
            long currentMessageId = _directReadStates.TryGetValue(key, out long readMessageId) ? readMessageId : 0;
            if (latestMessageId <= currentMessageId)
            {
                return;
            }

            _directReadStates[key] = latestMessageId;
            SaveDirectReadState(normalizedUser, normalizedFriend, latestMessageId);
            changed = true;
        }

        if (changed)
        {
            MessagesChanged?.Invoke();
        }
    }

    public ChatMessage? GetLatestVisibleMessage(int? guildId = null)
    {
        lock (_gate)
        {
            return _messages
                .Where(message => message.Channel != ChatChannel.Direct)
                .Where(message => message.Channel != ChatChannel.Guild || message.GuildId == guildId)
                .OrderByDescending(message => message.Id)
                .FirstOrDefault();
        }
    }

    public ChatMessage? GetLatestVisibleMessage(string? userName, int? guildId)
    {
        string normalizedUser = NormalizeUserName(userName);
        lock (_gate)
        {
            return _messages
                .Where(message => message.Channel != ChatChannel.Direct || IsDirectMessageVisibleTo(message, normalizedUser))
                .Where(message => message.Channel != ChatChannel.Guild || message.GuildId == guildId)
                .OrderByDescending(message => message.Id)
                .FirstOrDefault();
        }
    }

    public ChatSendResult SendWorldMessage(PlayerAccount? account, string? content)
        => SendPlayerMessage(account, ChatChannel.World, content, null, null);

    public ChatSendResult SendGuildMessage(PlayerAccount? account, Guild? guild, string? content)
    {
        if (guild is null)
        {
            return new ChatSendResult(false, "请先加入公会", null);
        }

        if (account is null
            || !guild.Members.Any(member => string.Equals(member.UserName, account.UserName, StringComparison.OrdinalIgnoreCase)))
        {
            return new ChatSendResult(false, "你不在这个公会", null);
        }

        return SendPlayerMessage(account, ChatChannel.Guild, content, guild.Id, null);
    }

    public ChatSendResult SendDirectMessage(PlayerAccount? account, string? recipientUserName, string? content)
    {
        if (account is null)
        {
            return new ChatSendResult(false, "请先登录", null);
        }

        if (account.Profile is null)
        {
            return new ChatSendResult(false, "请先创建角色", null);
        }

        string normalizedRecipient = NormalizeUserName(recipientUserName);
        if (string.IsNullOrWhiteSpace(normalizedRecipient))
        {
            return new ChatSendResult(false, "请选择好友", null);
        }

        if (SameUser(account.UserName, normalizedRecipient))
        {
            return new ChatSendResult(false, "不能给自己发送私聊", null);
        }

        PlayerAccount? recipient = _accounts.GetAccount(normalizedRecipient);
        if (recipient?.Profile is null)
        {
            return new ChatSendResult(false, "好友不存在", null);
        }

        if (!_friends.IsFriend(account.UserName, recipient.UserName))
        {
            return new ChatSendResult(false, "只能给好友发送私聊", null);
        }

        return SendPlayerMessage(account, ChatChannel.Direct, content, null, recipient.UserName);
    }

    public void AddSystemMessage(string content)
    {
        ChatMessage message;
        lock (_gate)
        {
            message = new ChatMessage(
                _nextMessageId++,
                ChatChannel.System,
                "system",
                "系统",
                NormalizeContent(content),
                DateTimeOffset.UtcNow,
                true);
            AddMessageCore(message);
            SaveMessage(message);
        }

        MessagesChanged?.Invoke();
    }

    private ChatSendResult SendPlayerMessage(PlayerAccount? account, ChatChannel channel, string? content, int? guildId, string? recipientUserName)
    {
        if (account is null)
        {
            return new ChatSendResult(false, "请先登录", null);
        }

        if (account.Profile is null)
        {
            return new ChatSendResult(false, "请先创建角色", null);
        }

        string normalized = NormalizeContent(content);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new ChatSendResult(false, "不能发送空消息", null);
        }

        if (normalized.Length > MaxMessageLength)
        {
            return new ChatSendResult(false, $"最多输入 {MaxMessageLength} 个字", null);
        }

        ChatMessage message;
        lock (_gate)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string cooldownKey = $"{account.UserName}:{channel}:{guildId}:{NormalizeUserName(recipientUserName)}";
            if (_lastSentAt.TryGetValue(cooldownKey, out DateTimeOffset lastSentAt)
                && now - lastSentAt < SendCooldown)
            {
                return new ChatSendResult(false, "发送太快了", null);
            }

            _lastSentAt[cooldownKey] = now;
            message = new ChatMessage(
                _nextMessageId++,
                channel,
                account.UserName,
                account.Profile.Name,
                normalized,
                now,
                false,
                guildId,
                recipientUserName);
            AddMessageCore(message);
            SaveMessage(message);
        }

        MessagesChanged?.Invoke();
        return new ChatSendResult(true, "发送成功", message);
    }

    private void LoadMessages()
    {
        ILiteCollection<ChatMessageDocument> collection = _database.GetCollection<ChatMessageDocument>("chat_messages");
        collection.EnsureIndex(message => message.Id, unique: true);

        foreach (ChatMessageDocument document in collection.FindAll().OrderBy(document => document.Id))
        {
            _messages.Add(ToMessage(document));
        }

        TrimStoredMessages();
        _nextMessageId = _messages.Select(message => message.Id).DefaultIfEmpty(0).Max() + 1;
    }

    private void LoadDirectReadStates()
    {
        ILiteCollection<DirectChatReadStateDocument> collection = _database.GetCollection<DirectChatReadStateDocument>("direct_chat_read_states");
        collection.EnsureIndex(state => state.UserName);
        collection.EnsureIndex(state => state.FriendUserName);

        foreach (DirectChatReadStateDocument document in collection.FindAll())
        {
            if (string.IsNullOrWhiteSpace(document.UserName) || string.IsNullOrWhiteSpace(document.FriendUserName))
            {
                continue;
            }

            _directReadStates[BuildDirectReadStateId(document.UserName, document.FriendUserName)] = Math.Max(0, document.LastReadMessageId);
        }
    }

    private void SaveMessage(ChatMessage message)
    {
        ILiteCollection<ChatMessageDocument> collection = _database.GetCollection<ChatMessageDocument>("chat_messages");
        collection.Upsert(new ChatMessageDocument
        {
            Id = message.Id,
            Channel = message.Channel,
            SenderUserName = message.SenderUserName,
            SenderDisplayName = message.SenderDisplayName,
            Content = message.Content,
            SentAt = message.SentAt,
            IsSystem = message.IsSystem,
            GuildId = message.GuildId,
            RecipientUserName = message.RecipientUserName,
        });
        DeleteOverflowDocuments(collection);
    }

    private void SaveDirectReadState(string userName, string friendUserName, long lastReadMessageId)
    {
        ILiteCollection<DirectChatReadStateDocument> collection = _database.GetCollection<DirectChatReadStateDocument>("direct_chat_read_states");
        collection.Upsert(new DirectChatReadStateDocument
        {
            Id = BuildDirectReadStateId(userName, friendUserName),
            UserName = NormalizeUserName(userName),
            FriendUserName = NormalizeUserName(friendUserName),
            LastReadMessageId = lastReadMessageId,
        });
    }

    private void AddMessageCore(ChatMessage message)
    {
        _messages.Add(message);
        TrimStoredMessages();
    }

    private void TrimStoredMessages()
    {
        if (_messages.Count <= MaxStoredMessages)
        {
            return;
        }

        int removeCount = _messages.Count - MaxStoredMessages;
        _messages.RemoveRange(0, removeCount);
    }

    private static void DeleteOverflowDocuments(ILiteCollection<ChatMessageDocument> collection)
    {
        List<long> ids = collection.FindAll()
            .OrderByDescending(message => message.Id)
            .Skip(MaxStoredMessages)
            .Select(message => message.Id)
            .ToList();

        foreach (long id in ids)
        {
            collection.Delete(id);
        }
    }

    private static string NormalizeContent(string? content)
        => string.Join(
            ' ',
            (content ?? string.Empty)
                .Trim()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private IEnumerable<ChatMessage> GetDirectMessagesCore(string userName, string friendUserName)
        => _messages
            .Where(message => message.Channel == ChatChannel.Direct)
            .Where(message =>
                (SameUser(message.SenderUserName, userName) && SameUser(message.RecipientUserName, friendUserName))
                || (SameUser(message.SenderUserName, friendUserName) && SameUser(message.RecipientUserName, userName)));

    private int GetUnreadDirectCountCore(string userName, string friendUserName, long lastReadMessageId)
        => _messages
            .Where(message => message.Channel == ChatChannel.Direct)
            .Where(message => SameUser(message.SenderUserName, friendUserName) && SameUser(message.RecipientUserName, userName))
            .Count(message => message.Id > lastReadMessageId);

    private long GetLastReadMessageIdCore(string userName, string friendUserName)
        => _directReadStates.TryGetValue(BuildDirectReadStateId(userName, friendUserName), out long messageId)
            ? messageId
            : 0;

    private static ChatMessage ToMessage(ChatMessageDocument document)
        => new(
            document.Id,
            document.Channel,
            document.SenderUserName,
            document.SenderDisplayName,
            document.Content,
            document.SentAt,
            document.IsSystem,
            document.GuildId,
            document.RecipientUserName);

    private static bool IsDirectMessageVisibleTo(ChatMessage message, string userName)
        => !string.IsNullOrWhiteSpace(userName)
            && (SameUser(message.SenderUserName, userName) || SameUser(message.RecipientUserName, userName));

    private static string NormalizeUserName(string? userName)
        => userName?.Trim() ?? string.Empty;

    private static bool SameUser(string? left, string? right)
        => string.Equals(NormalizeUserName(left), NormalizeUserName(right), StringComparison.OrdinalIgnoreCase);

    private static string BuildDirectReadStateId(string userName, string friendUserName)
        => $"{NormalizeUserName(userName).ToLowerInvariant()}:{NormalizeUserName(friendUserName).ToLowerInvariant()}";
}
