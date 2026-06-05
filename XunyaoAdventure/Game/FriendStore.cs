using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class FriendStore
{
    public const int MaxFriends = 80;

    private readonly object _gate = new();
    private readonly GameDatabase _database;
    private readonly GameAccountStore _accounts;
    private readonly List<FriendRelation> _relations = new();
    private readonly List<FriendRequest> _requests = new();

    public event Action? FriendsChanged;

    public FriendStore(GameDatabase database, GameAccountStore accounts)
    {
        _database = database;
        _accounts = accounts;
        LoadRelations();
        LoadRequests();
    }

    public IReadOnlyList<FriendProfile> GetFriends(string? userName)
    {
        string normalized = Normalize(userName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<FriendProfile>();
        }

        lock (_gate)
        {
            return _relations
                .Where(relation => SameUser(relation.UserName, normalized))
                .Select(relation => CreateFriendProfile(relation))
                .Where(profile => profile is not null)
                .Select(profile => profile!)
                .OrderBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(profile => profile.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<FriendRequestProfile> GetIncomingRequests(string? userName)
    {
        string normalized = Normalize(userName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<FriendRequestProfile>();
        }

        lock (_gate)
        {
            return _requests
                .Where(request => SameUser(request.ToUserName, normalized))
                .Select(CreateRequestProfile)
                .Where(profile => profile is not null)
                .Select(profile => profile!)
                .OrderByDescending(profile => profile.CreatedAt)
                .ToList();
        }
    }

    public IReadOnlyList<PlayerAccount> SearchPlayers(string? currentUserName, string? keyword, int limit = 20)
    {
        string normalizedCurrent = Normalize(currentUserName);
        string normalizedKeyword = Normalize(keyword);
        if (string.IsNullOrWhiteSpace(normalizedCurrent) || string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return Array.Empty<PlayerAccount>();
        }

        return _accounts.GetAccounts()
            .Where(account => account.Profile is not null)
            .Where(account => !SameUser(account.UserName, normalizedCurrent))
            .Where(account =>
                account.UserName.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase)
                || (account.Profile?.Name.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(account => IsFriend(normalizedCurrent, account.UserName))
            .ThenBy(account => account.Profile?.Name ?? account.UserName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, limit))
            .ToList();
    }

    public bool IsFriend(string? userName, string? friendUserName)
    {
        string normalizedUser = Normalize(userName);
        string normalizedFriend = Normalize(friendUserName);
        if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(normalizedFriend))
        {
            return false;
        }

        lock (_gate)
        {
            return _relations.Any(relation =>
                SameUser(relation.UserName, normalizedUser)
                && SameUser(relation.FriendUserName, normalizedFriend));
        }
    }

    public bool HasIncomingRequest(string? userName, string? fromUserName)
    {
        string normalizedUser = Normalize(userName);
        string normalizedFrom = Normalize(fromUserName);
        if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(normalizedFrom))
        {
            return false;
        }

        lock (_gate)
        {
            return _requests.Any(request =>
                SameUser(request.ToUserName, normalizedUser)
                && SameUser(request.FromUserName, normalizedFrom));
        }
    }

    public bool HasOutgoingRequest(string? userName, string? toUserName)
    {
        string normalizedUser = Normalize(userName);
        string normalizedTo = Normalize(toUserName);
        if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(normalizedTo))
        {
            return false;
        }

        lock (_gate)
        {
            return _requests.Any(request =>
                SameUser(request.FromUserName, normalizedUser)
                && SameUser(request.ToUserName, normalizedTo));
        }
    }

    public FriendActionResult SendRequest(string? fromUserName, string? toUserName)
    {
        string normalizedFrom = Normalize(fromUserName);
        string normalizedTo = Normalize(toUserName);
        if (string.IsNullOrWhiteSpace(normalizedFrom))
        {
            return new FriendActionResult(false, "请先登录");
        }

        if (string.IsNullOrWhiteSpace(normalizedTo))
        {
            return new FriendActionResult(false, "请输入玩家账号");
        }

        if (SameUser(normalizedFrom, normalizedTo))
        {
            return new FriendActionResult(false, "不能添加自己为好友");
        }

        PlayerAccount? fromAccount = _accounts.GetAccount(normalizedFrom);
        PlayerAccount? toAccount = _accounts.GetAccount(normalizedTo);
        if (fromAccount?.Profile is null)
        {
            return new FriendActionResult(false, "请先创建角色");
        }

        if (toAccount?.Profile is null)
        {
            return new FriendActionResult(false, "玩家不存在或尚未创建角色");
        }

        bool changed = false;
        FriendActionResult result;
        lock (_gate)
        {
            if (IsFriendCore(normalizedFrom, normalizedTo))
            {
                result = new FriendActionResult(false, "已经是好友");
            }
            else if (GetFriendCountCore(normalizedFrom) >= MaxFriends)
            {
                result = new FriendActionResult(false, $"好友数量已达上限 {MaxFriends}");
            }
            else if (GetFriendCountCore(normalizedTo) >= MaxFriends)
            {
                result = new FriendActionResult(false, "对方好友数量已达上限");
            }
            else if (FindRequestIndex(normalizedTo, normalizedFrom) >= 0)
            {
                AddFriendPair(normalizedFrom, normalizedTo, DateTimeOffset.UtcNow);
                DeleteRequest(normalizedTo, normalizedFrom);
                changed = true;
                result = new FriendActionResult(true, "已接受对方申请，添加好友成功");
            }
            else if (FindRequestIndex(normalizedFrom, normalizedTo) >= 0)
            {
                result = new FriendActionResult(false, "好友申请已发送");
            }
            else
            {
                FriendRequest request = new(normalizedFrom, normalizedTo, DateTimeOffset.UtcNow);
                _requests.Add(request);
                SaveRequest(request);
                changed = true;
                result = new FriendActionResult(true, "好友申请已发送");
            }
        }

        if (changed)
        {
            FriendsChanged?.Invoke();
        }

        return result;
    }

    public FriendActionResult AcceptRequest(string? userName, string? fromUserName)
    {
        string normalizedUser = Normalize(userName);
        string normalizedFrom = Normalize(fromUserName);
        if (string.IsNullOrWhiteSpace(normalizedUser))
        {
            return new FriendActionResult(false, "请先登录");
        }

        bool changed = false;
        FriendActionResult result;
        lock (_gate)
        {
            int requestIndex = FindRequestIndex(normalizedFrom, normalizedUser);
            if (requestIndex < 0)
            {
                result = new FriendActionResult(false, "好友申请不存在");
            }
            else if (GetFriendCountCore(normalizedUser) >= MaxFriends)
            {
                result = new FriendActionResult(false, $"好友数量已达上限 {MaxFriends}");
            }
            else if (GetFriendCountCore(normalizedFrom) >= MaxFriends)
            {
                result = new FriendActionResult(false, "对方好友数量已达上限");
            }
            else
            {
                AddFriendPair(normalizedUser, normalizedFrom, DateTimeOffset.UtcNow);
                DeleteRequest(normalizedFrom, normalizedUser);
                changed = true;
                result = new FriendActionResult(true, "已添加好友");
            }
        }

        if (changed)
        {
            FriendsChanged?.Invoke();
        }

        return result;
    }

    public FriendActionResult RejectRequest(string? userName, string? fromUserName)
    {
        string normalizedUser = Normalize(userName);
        string normalizedFrom = Normalize(fromUserName);
        if (string.IsNullOrWhiteSpace(normalizedUser))
        {
            return new FriendActionResult(false, "请先登录");
        }

        bool changed = false;
        FriendActionResult result;
        lock (_gate)
        {
            if (FindRequestIndex(normalizedFrom, normalizedUser) < 0)
            {
                result = new FriendActionResult(false, "好友申请不存在");
            }
            else
            {
                DeleteRequest(normalizedFrom, normalizedUser);
                changed = true;
                result = new FriendActionResult(true, "已拒绝申请");
            }
        }

        if (changed)
        {
            FriendsChanged?.Invoke();
        }

        return result;
    }

    public FriendActionResult RemoveFriend(string? userName, string? friendUserName)
    {
        string normalizedUser = Normalize(userName);
        string normalizedFriend = Normalize(friendUserName);
        if (string.IsNullOrWhiteSpace(normalizedUser))
        {
            return new FriendActionResult(false, "请先登录");
        }

        bool changed = false;
        FriendActionResult result;
        lock (_gate)
        {
            if (!IsFriendCore(normalizedUser, normalizedFriend))
            {
                result = new FriendActionResult(false, "你们还不是好友");
            }
            else
            {
                DeleteRelation(normalizedUser, normalizedFriend);
                DeleteRelation(normalizedFriend, normalizedUser);
                changed = true;
                result = new FriendActionResult(true, "已删除好友");
            }
        }

        if (changed)
        {
            FriendsChanged?.Invoke();
        }

        return result;
    }

    private void LoadRelations()
    {
        ILiteCollection<FriendRelationDocument> collection = _database.GetCollection<FriendRelationDocument>("friend_relations");
        collection.EnsureIndex(relation => relation.UserName);
        collection.EnsureIndex(relation => relation.FriendUserName);

        foreach (FriendRelationDocument document in collection.FindAll())
        {
            if (string.IsNullOrWhiteSpace(document.UserName) || string.IsNullOrWhiteSpace(document.FriendUserName))
            {
                continue;
            }

            _relations.Add(new FriendRelation(document.UserName, document.FriendUserName, document.CreatedAt));
        }
    }

    private void LoadRequests()
    {
        ILiteCollection<FriendRequestDocument> collection = _database.GetCollection<FriendRequestDocument>("friend_requests");
        collection.EnsureIndex(request => request.FromUserName);
        collection.EnsureIndex(request => request.ToUserName);

        foreach (FriendRequestDocument document in collection.FindAll())
        {
            if (string.IsNullOrWhiteSpace(document.FromUserName) || string.IsNullOrWhiteSpace(document.ToUserName))
            {
                continue;
            }

            _requests.Add(new FriendRequest(document.FromUserName, document.ToUserName, document.CreatedAt));
        }
    }

    private FriendProfile? CreateFriendProfile(FriendRelation relation)
    {
        PlayerAccount? account = _accounts.GetAccount(relation.FriendUserName);
        if (account?.Profile is null)
        {
            return null;
        }

        return new FriendProfile(
            account.UserName,
            account.Profile.Name,
            account.Level,
            account.Monsters.Sum(monster => monster.Power),
            relation.CreatedAt);
    }

    private FriendRequestProfile? CreateRequestProfile(FriendRequest request)
    {
        PlayerAccount? account = _accounts.GetAccount(request.FromUserName);
        if (account?.Profile is null)
        {
            return null;
        }

        return new FriendRequestProfile(
            account.UserName,
            account.Profile.Name,
            account.Level,
            account.Monsters.Sum(monster => monster.Power),
            request.CreatedAt);
    }

    private void AddFriendPair(string userName, string friendUserName, DateTimeOffset createdAt)
    {
        AddRelation(userName, friendUserName, createdAt);
        AddRelation(friendUserName, userName, createdAt);
    }

    private void AddRelation(string userName, string friendUserName, DateTimeOffset createdAt)
    {
        if (IsFriendCore(userName, friendUserName))
        {
            return;
        }

        FriendRelation relation = new(userName, friendUserName, createdAt);
        _relations.Add(relation);
        SaveRelation(relation);
    }

    private void SaveRelation(FriendRelation relation)
    {
        ILiteCollection<FriendRelationDocument> collection = _database.GetCollection<FriendRelationDocument>("friend_relations");
        collection.Upsert(new FriendRelationDocument
        {
            Id = BuildRelationId(relation.UserName, relation.FriendUserName),
            UserName = relation.UserName,
            FriendUserName = relation.FriendUserName,
            CreatedAt = relation.CreatedAt,
        });
    }

    private void SaveRequest(FriendRequest request)
    {
        ILiteCollection<FriendRequestDocument> collection = _database.GetCollection<FriendRequestDocument>("friend_requests");
        collection.Upsert(new FriendRequestDocument
        {
            Id = BuildRelationId(request.FromUserName, request.ToUserName),
            FromUserName = request.FromUserName,
            ToUserName = request.ToUserName,
            CreatedAt = request.CreatedAt,
        });
    }

    private void DeleteRelation(string userName, string friendUserName)
    {
        int index = _relations.FindIndex(relation =>
            SameUser(relation.UserName, userName)
            && SameUser(relation.FriendUserName, friendUserName));
        if (index < 0)
        {
            return;
        }

        _relations.RemoveAt(index);
        _database.GetCollection<FriendRelationDocument>("friend_relations")
            .Delete(BuildRelationId(userName, friendUserName));
    }

    private void DeleteRequest(string fromUserName, string toUserName)
    {
        int index = FindRequestIndex(fromUserName, toUserName);
        if (index >= 0)
        {
            _requests.RemoveAt(index);
        }

        _database.GetCollection<FriendRequestDocument>("friend_requests")
            .Delete(BuildRelationId(fromUserName, toUserName));
    }

    private bool IsFriendCore(string userName, string friendUserName)
        => _relations.Any(relation =>
            SameUser(relation.UserName, userName)
            && SameUser(relation.FriendUserName, friendUserName));

    private int GetFriendCountCore(string userName)
        => _relations.Count(relation => SameUser(relation.UserName, userName));

    private int FindRequestIndex(string fromUserName, string toUserName)
        => _requests.FindIndex(request =>
            SameUser(request.FromUserName, fromUserName)
            && SameUser(request.ToUserName, toUserName));

    private static string BuildRelationId(string userName, string friendUserName)
        => $"{Normalize(userName).ToLowerInvariant()}:{Normalize(friendUserName).ToLowerInvariant()}";

    private static string Normalize(string? value)
        => value?.Trim() ?? string.Empty;

    private static bool SameUser(string? left, string? right)
        => string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private sealed record FriendRelation(string UserName, string FriendUserName, DateTimeOffset CreatedAt);

    private sealed record FriendRequest(string FromUserName, string ToUserName, DateTimeOffset CreatedAt);
}
