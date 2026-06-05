namespace XunyaoAdventure.Game;

public sealed record FriendProfile(
    string UserName,
    string DisplayName,
    int Level,
    int TotalPower,
    DateTimeOffset AddedAt);

public sealed record FriendRequestProfile(
    string FromUserName,
    string FromDisplayName,
    int FromLevel,
    int FromTotalPower,
    DateTimeOffset CreatedAt);

public sealed record FriendActionResult(bool Success, string Message);
