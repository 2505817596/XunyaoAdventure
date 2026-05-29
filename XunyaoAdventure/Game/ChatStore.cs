using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class ChatStore
{
    public const int MaxMessageLength = 80;
    public const int MaxStoredMessages = 120;

    private static readonly TimeSpan SendCooldown = TimeSpan.FromMilliseconds(800);

    private readonly object _gate = new();
    private readonly GameDatabase _database;
    private readonly List<ChatMessage> _messages = new();
    private readonly Dictionary<string, DateTimeOffset> _lastSentAt = new(StringComparer.OrdinalIgnoreCase);
    private long _nextMessageId = 1;

    public event Action? MessagesChanged;

    public ChatStore(GameDatabase database)
    {
        _database = database;
        LoadMessages();

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
                .OrderBy(message => message.Id)
                .ToList();
        }
    }

    public ChatMessage? GetLatestVisibleMessage(int? guildId = null)
    {
        lock (_gate)
        {
            return _messages
                .Where(message => message.Channel != ChatChannel.Guild || message.GuildId == guildId)
                .OrderByDescending(message => message.Id)
                .FirstOrDefault();
        }
    }

    public ChatSendResult SendWorldMessage(PlayerAccount? account, string? content)
        => SendPlayerMessage(account, ChatChannel.World, content, null);

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

        return SendPlayerMessage(account, ChatChannel.Guild, content, guild.Id);
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

    private ChatSendResult SendPlayerMessage(PlayerAccount? account, ChatChannel channel, string? content, int? guildId)
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
            string cooldownKey = $"{account.UserName}:{channel}:{guildId}";
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
                guildId);
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
        });
        DeleteOverflowDocuments(collection);
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

    private static ChatMessage ToMessage(ChatMessageDocument document)
        => new(
            document.Id,
            document.Channel,
            document.SenderUserName,
            document.SenderDisplayName,
            document.Content,
            document.SentAt,
            document.IsSystem,
            document.GuildId);
}
