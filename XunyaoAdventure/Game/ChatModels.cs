namespace XunyaoAdventure.Game;

public enum ChatChannel
{
    World,
    Guild,
    Direct,
    System,
}

public sealed record ChatMessage(
    long Id,
    ChatChannel Channel,
    string SenderUserName,
    string SenderDisplayName,
    string Content,
    DateTimeOffset SentAt,
    bool IsSystem,
    int? GuildId = null,
    string? RecipientUserName = null);

public sealed record ChatSendResult(bool Success, string Message, ChatMessage? ChatMessage);

public sealed record DirectChatSummary(
    string FriendUserName,
    ChatMessage? LatestMessage,
    int UnreadCount);
