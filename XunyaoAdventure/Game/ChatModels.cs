namespace XunyaoAdventure.Game;

public enum ChatChannel
{
    World,
    Guild,
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
    int? GuildId = null);

public sealed record ChatSendResult(bool Success, string Message, ChatMessage? ChatMessage);
