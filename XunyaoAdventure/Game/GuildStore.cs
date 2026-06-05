using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class GuildStore
{
    private const int MaxGuildNameLength = 12;
    private const int MaxNoticeLength = 80;
    private const int MaxContributionAmount = 1000000;

    private readonly object _gate = new();
    private readonly GameAccountStore _accounts;
    private readonly GameConfigStore _configs;
    private readonly GameDatabase _database;
    private readonly List<Guild> _guilds = new();
    private int _nextGuildId = 1;

    public GuildStore(GameAccountStore accounts, GameConfigStore configs, GameDatabase database)
    {
        _accounts = accounts;
        _configs = configs;
        _database = database;
        LoadGuilds();
    }

    public IReadOnlyList<Guild> GetGuilds()
    {
        lock (_gate)
        {
            return _guilds.Select(CloneGuild).ToList();
        }
    }

    public Guild? GetGuild(int guildId)
    {
        lock (_gate)
        {
            Guild? guild = _guilds.FirstOrDefault(item => item.Id == guildId);
            return guild is null ? null : CloneGuild(guild);
        }
    }

    public Guild? GetGuildByMember(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        lock (_gate)
        {
            Guild? guild = FindGuildByMember(userName);
            return guild is null ? null : CloneGuild(guild);
        }
    }

    public GuildActionResult CreateGuild(string? userName, string name)
    {
        PlayerAccount? account = _accounts.GetAccount(userName);
        if (account?.Profile is null)
        {
            return new GuildActionResult(false, "请先创建角色", null);
        }

        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return new GuildActionResult(false, "公会名称不能为空", null);
        }

        if (name.Length > MaxGuildNameLength)
        {
            return new GuildActionResult(false, $"公会名称最多{MaxGuildNameLength}个字", null);
        }

        lock (_gate)
        {
            if (FindGuildByMember(account.UserName) is not null)
            {
                return new GuildActionResult(false, "你已经加入公会", null);
            }

            if (_guilds.Any(guild => string.Equals(guild.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return new GuildActionResult(false, "公会名称已存在", null);
            }

            Guild guild = new(
                _nextGuildId++,
                name,
                string.Empty,
                account.UserName,
                new List<GuildMember>
                {
                    new(account.UserName, account.Profile.Name, GuildMemberRole.Leader, DateTime.UtcNow, 0),
                },
                DateTime.UtcNow,
                0,
                1);

            _guilds.Add(guild);
            SaveGuild(guild);
            return new GuildActionResult(true, "创建成功", CloneGuild(guild));
        }
    }

    public GuildActionResult JoinGuild(string? userName, int guildId)
    {
        PlayerAccount? account = _accounts.GetAccount(userName);
        if (account?.Profile is null)
        {
            return new GuildActionResult(false, "请先创建角色", null);
        }

        lock (_gate)
        {
            if (FindGuildByMember(account.UserName) is not null)
            {
                return new GuildActionResult(false, "你已经加入公会", null);
            }

            Guild? guild = _guilds.FirstOrDefault(item => item.Id == guildId);
            if (guild is null)
            {
                return new GuildActionResult(false, "公会不存在", null);
            }

            guild.Members.Add(new GuildMember(account.UserName, account.Profile.Name, GuildMemberRole.Member, DateTime.UtcNow, 0));
            SaveGuild(guild);
            return new GuildActionResult(true, "加入成功", CloneGuild(guild));
        }
    }

    public GuildActionResult LeaveGuild(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new GuildActionResult(false, "请先登录", null);
        }

        lock (_gate)
        {
            Guild? guild = FindGuildByMember(userName);
            if (guild is null)
            {
                return new GuildActionResult(false, "你还没有加入公会", null);
            }

            int memberIndex = guild.Members.FindIndex(member =>
                string.Equals(member.UserName, userName, StringComparison.OrdinalIgnoreCase));
            if (memberIndex < 0)
            {
                return new GuildActionResult(false, "成员不存在", CloneGuild(guild));
            }

            bool leavingLeader = string.Equals(guild.LeaderUserName, userName, StringComparison.OrdinalIgnoreCase);
            guild.Members.RemoveAt(memberIndex);
            if (guild.Members.Count == 0)
            {
                _guilds.Remove(guild);
                DeleteGuild(guild.Id);
                return new GuildActionResult(true, "已退出公会，公会已解散", null);
            }

            if (leavingLeader)
            {
                GuildMember nextLeader = guild.Members.OrderBy(member => member.JoinedAt).First();
                int nextLeaderIndex = guild.Members.FindIndex(member => member.UserName == nextLeader.UserName);
                guild.Members[nextLeaderIndex].Role = GuildMemberRole.Leader;
                guild.LeaderUserName = nextLeader.UserName;
                SaveGuild(guild);
                return new GuildActionResult(true, "已退出公会，会长已转让", CloneGuild(guild));
            }

            SaveGuild(guild);
            return new GuildActionResult(true, "已退出公会", CloneGuild(guild));
        }
    }

    public GuildActionResult UpdateNotice(string? userName, string notice)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new GuildActionResult(false, "请先登录", null);
        }

        notice = notice.Trim();
        if (notice.Length > MaxNoticeLength)
        {
            return new GuildActionResult(false, $"公告最多{MaxNoticeLength}个字", null);
        }

        lock (_gate)
        {
            Guild? guild = FindGuildByMember(userName);
            if (guild is null)
            {
                return new GuildActionResult(false, "你还没有加入公会", null);
            }

            if (!string.Equals(guild.LeaderUserName, userName, StringComparison.OrdinalIgnoreCase))
            {
                return new GuildActionResult(false, "只有会长可以修改公告", CloneGuild(guild));
            }

            guild.Notice = notice;
            SaveGuild(guild);
            return new GuildActionResult(true, "公告已保存", CloneGuild(guild));
        }
    }

    public GuildContributionResult ContributeCopper(string? userName, int amount)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new GuildContributionResult(false, "请先登录", null, null);
        }

        amount = Math.Clamp(amount, 0, MaxContributionAmount);
        if (amount <= 0)
        {
            return new GuildContributionResult(false, "贡献数量必须大于0", GetGuildByMember(userName), _accounts.GetAccount(userName));
        }

        Guild? guildSnapshot;
        lock (_gate)
        {
            guildSnapshot = FindGuildByMember(userName);
            if (guildSnapshot is null)
            {
                return new GuildContributionResult(false, "你还没有加入公会", null, _accounts.GetAccount(userName));
            }
        }

        if (!_accounts.TrySpendCopper(userName, amount, out PlayerAccount? account, out string message))
        {
            return new GuildContributionResult(false, message, guildSnapshot is null ? null : CloneGuild(guildSnapshot), account);
        }

        lock (_gate)
        {
            Guild? guild = FindGuildByMember(userName);
            if (guild is null)
            {
                _accounts.GrantRewards(userName, new[] { new QuestReward(QuestRewardType.Copper, "铜钱", amount) });
                return new GuildContributionResult(false, "你还没有加入公会", null, _accounts.GetAccount(userName));
            }

            int memberIndex = guild.Members.FindIndex(member =>
                string.Equals(member.UserName, userName, StringComparison.OrdinalIgnoreCase));
            if (memberIndex < 0)
            {
                _accounts.GrantRewards(userName, new[] { new QuestReward(QuestRewardType.Copper, "铜钱", amount) });
                return new GuildContributionResult(false, "成员不存在", CloneGuild(guild), _accounts.GetAccount(userName));
            }

            int oldLevel = guild.Level;
            guild.TotalContribution += amount;
            guild.Members[memberIndex].Contribution += amount;
            guild.Level = ResolveGuildLevel(guild.TotalContribution, _configs.GetGuildConfig());
            SaveGuild(guild);
            string resultMessage = guild.Level > oldLevel
                ? $"贡献成功，公会升至{guild.Level}级"
                : $"贡献成功，增加{amount}贡献";
            return new GuildContributionResult(true, resultMessage, CloneGuild(guild), account);
        }
    }

    public static int ResolveGuildLevel(int totalContribution)
        => ResolveGuildLevel(totalContribution, new GuildConfig());

    private static int ResolveGuildLevel(int totalContribution, GuildConfig config)
    {
        int maxLevel = Math.Max(1, config.MaxLevel);
        int level = 1;
        if (config.LevelRequirements.Count == 0)
        {
            while (level < maxLevel && totalContribution >= GetRequiredTotalContributionForLevel(level + 1, config))
            {
                level++;
            }

            return level;
        }

        foreach (GuildLevelRequirementConfig requirement in config.LevelRequirements.OrderBy(requirement => requirement.Level))
        {
            if (requirement.Level > maxLevel)
            {
                break;
            }

            if (totalContribution < requirement.RequiredTotalContribution)
            {
                break;
            }

            level = Math.Max(level, requirement.Level);
        }

        return Math.Min(level, maxLevel);
    }

    public static int GetRequiredTotalContributionForLevel(int level)
        => GetRequiredTotalContributionForLevel(level, new GuildConfig());

    private static int GetRequiredTotalContributionForLevel(int level, GuildConfig config)
    {
        if (level <= 1)
        {
            return 0;
        }

        GuildLevelRequirementConfig? requirement = config.LevelRequirements.FirstOrDefault(item => item.Level == level);
        return requirement?.RequiredTotalContribution ?? Math.Max(1, (level - 1) * 1000);
    }

    public static int GetRequiredTotalContributionForNextLevel(int level)
        => GetRequiredTotalContributionForNextLevel(level, new GuildConfig());

    public int GetConfiguredRequiredTotalContributionForNextLevel(int level)
    {
        GuildConfig config = _configs.GetGuildConfig();
        return GetRequiredTotalContributionForNextLevel(level, config);
    }

    private static int GetRequiredTotalContributionForNextLevel(int level, GuildConfig config)
        => level >= Math.Max(1, config.MaxLevel)
            ? 0
            : GetRequiredTotalContributionForLevel(level + 1, config);

    private Guild? FindGuildByMember(string userName)
        => _guilds.FirstOrDefault(guild =>
            guild.Members.Any(member => string.Equals(member.UserName, userName, StringComparison.OrdinalIgnoreCase)));

    private void LoadGuilds()
    {
        ILiteCollection<GuildDocument> collection = _database.GetCollection<GuildDocument>("guilds");
        collection.EnsureIndex(guild => guild.Id, unique: true);

        foreach (GuildDocument document in collection.FindAll())
        {
            _guilds.Add(CloneGuild(ToGuild(document)));
        }

        _nextGuildId = _guilds.Select(guild => guild.Id).DefaultIfEmpty(0).Max() + 1;
    }

    private void SaveGuild(Guild guild)
    {
        GuildConfig config = _configs.GetGuildConfig();
        guild.Level = ResolveGuildLevel(guild.TotalContribution, config);
        ILiteCollection<GuildDocument> collection = _database.GetCollection<GuildDocument>("guilds");
        collection.Upsert(new GuildDocument
        {
            Id = guild.Id,
            Name = guild.Name,
            Notice = guild.Notice,
            LeaderUserName = guild.LeaderUserName,
            Level = guild.Level,
            Members = guild.Members
                .Select(member => new GuildMemberDocument
                {
                    UserName = member.UserName,
                    RoleName = member.RoleName,
                    Role = member.Role,
                    JoinedAt = member.JoinedAt,
                    Contribution = member.Contribution,
            })
                .ToList(),
            CreatedAt = guild.CreatedAt,
            TotalContribution = guild.TotalContribution,
        });
    }

    private void DeleteGuild(int guildId)
    {
        ILiteCollection<GuildDocument> collection = _database.GetCollection<GuildDocument>("guilds");
        collection.Delete(guildId);
    }

    private Guild CloneGuild(Guild guild)
        => new(
            guild.Id,
            guild.Name,
            guild.Notice,
            guild.LeaderUserName,
            guild.Members
                .Select(member => new GuildMember(member.UserName, member.RoleName, member.Role, member.JoinedAt, member.Contribution))
                .ToList(),
            guild.CreatedAt,
            guild.TotalContribution,
            ResolveGuildLevel(guild.TotalContribution, _configs.GetGuildConfig()));

    private Guild ToGuild(GuildDocument document)
    {
        int totalContribution = Math.Max(0, document.TotalContribution);
        return new Guild(
            document.Id,
            document.Name,
            document.Notice,
            document.LeaderUserName,
            document.Members
                .Select(member => new GuildMember(member.UserName, member.RoleName, member.Role, member.JoinedAt, member.Contribution))
                .ToList(),
            document.CreatedAt,
            totalContribution,
            ResolveGuildLevel(totalContribution, _configs.GetGuildConfig()));
    }
}
