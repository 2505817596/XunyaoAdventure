using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class ArenaStore
{
    private const int InitialScore = 1000;
    private const int PlacementPowerDivisor = 25;
    private const int WinBaseScore = 18;
    private const int LoseBaseScore = 10;

    private readonly object _gate = new();
    private readonly GameDatabase _database;
    private readonly GameAccountStore _accounts;
    private readonly Dictionary<string, ArenaProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public ArenaStore(GameDatabase database, GameAccountStore accounts)
    {
        _database = database;
        _accounts = accounts;
        LoadProfiles();
    }

    public ArenaProfile? GetProfile(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        PlayerAccount? account = _accounts.GetAccount(userName);
        if (account?.Profile is null)
        {
            return null;
        }

        lock (_gate)
        {
            return EnsureProfile(account);
        }
    }

    public IReadOnlyList<ArenaRankingEntry> GetRankings(int limit = 50)
    {
        lock (_gate)
        {
            EnsureProfilesForCreatedCharacters();
            return BuildRankings()
                .Take(Math.Max(1, limit))
                .ToList();
        }
    }

    public IReadOnlyList<ArenaOpponent> GetOpponents(string? userName, int count = 3)
    {
        PlayerAccount? account = _accounts.GetAccount(userName);
        if (account?.Profile is null)
        {
            return Array.Empty<ArenaOpponent>();
        }

        lock (_gate)
        {
            ArenaProfile profile = EnsureProfile(account);
            List<ArenaRankingEntry> rankings = BuildRankings();
            int selfRank = rankings.FirstOrDefault(entry => IsSameUser(entry.UserName, profile.UserName))?.Rank ?? rankings.Count;
            HashSet<string> selectedUsers = new(StringComparer.OrdinalIgnoreCase);

            List<ArenaOpponent> opponents = rankings
                .Where(entry => !IsSameUser(entry.UserName, profile.UserName))
                .OrderBy(entry => Math.Abs(entry.Rank - selfRank))
                .ThenByDescending(entry => entry.Score)
                .Select(entry => CreateOpponent(entry.UserName, entry.Rank))
                .Where(opponent => opponent is not null && selectedUsers.Add(opponent.UserName))
                .Take(Math.Max(1, count))
                .Cast<ArenaOpponent>()
                .ToList();

            if (opponents.Count < count)
            {
                foreach (PlayerAccount botAccount in CreatePracticeOpponents(account, count - opponents.Count))
                {
                    ArenaOpponent opponent = CreatePracticeOpponent(botAccount, profile.Score, opponents.Count + 1);
                    if (selectedUsers.Add(opponent.UserName))
                    {
                        opponents.Add(opponent);
                    }
                }
            }

            return opponents;
        }
    }

    public ArenaBattleSelection? CreateBattleSelection(string? userName, string opponentUserName)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(opponentUserName))
        {
            return null;
        }

        PlayerAccount? account = _accounts.GetAccount(userName);
        if (account?.Profile is null)
        {
            return null;
        }

        lock (_gate)
        {
            EnsureProfile(account);
            ArenaOpponent? opponent = GetOpponents(userName, 12)
                .FirstOrDefault(item => IsSameUser(item.UserName, opponentUserName));
            if (opponent is null)
            {
                return null;
            }

            return new ArenaBattleSelection(
                opponent.UserName,
                opponent.RoleName,
                opponent.Score,
                opponent.Rank,
                opponent.TotalPower,
                opponent.Formation.ToList());
        }
    }

    public ArenaChallengeResult CompleteChallenge(string? userName, ArenaBattleSelection? selection, bool victory)
    {
        PlayerAccount? account = _accounts.GetAccount(userName);
        if (account?.Profile is null || selection is null)
        {
            return new ArenaChallengeResult(false, "竞技场战斗已失效", null, null);
        }

        lock (_gate)
        {
            ArenaProfile profile = EnsureProfile(account);
            int oldScore = profile.Score;
            int oldRank = ResolveRank(profile.UserName);
            int scoreDelta = ResolveScoreDelta(profile.Score, selection.OpponentScore, victory);

            profile.Score = Math.Max(0, profile.Score + scoreDelta);
            profile.ChallengeCount++;
            if (victory)
            {
                profile.Wins++;
            }
            else
            {
                profile.Losses++;
            }

            profile.RoleName = account.Profile.Name;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            SaveProfile(profile);

            ArenaBattleResult result = new(
                victory,
                selection.OpponentUserName,
                selection.OpponentRoleName,
                scoreDelta,
                oldScore,
                profile.Score,
                oldRank,
                ResolveRank(profile.UserName),
                profile.Wins,
                profile.Losses);

            return new ArenaChallengeResult(true, victory ? "挑战胜利" : "挑战失败", profile, result);
        }
    }

    private ArenaProfile EnsureProfile(PlayerAccount account)
    {
        if (_profiles.TryGetValue(account.UserName, out ArenaProfile? profile))
        {
            string roleName = account.Profile?.Name ?? account.UserName;
            if (!string.Equals(profile.RoleName, roleName, StringComparison.Ordinal))
            {
                profile.RoleName = roleName;
                SaveProfile(profile);
            }

            return profile;
        }

        profile = new ArenaProfile(
            account.UserName,
            account.Profile?.Name ?? account.UserName,
            InitialScore + ResolveTotalPower(account) / PlacementPowerDivisor,
            0,
            0,
            0,
            DateTimeOffset.UtcNow);
        SaveProfile(profile);
        return profile;
    }

    private void EnsureProfilesForCreatedCharacters()
    {
        foreach (PlayerAccount account in _accounts.GetAccounts().Where(item => item.Profile is not null))
        {
            EnsureProfile(account);
        }
    }

    private List<ArenaRankingEntry> BuildRankings()
        => _profiles.Values
            .Select(profile =>
            {
                PlayerAccount? account = _accounts.GetAccount(profile.UserName);
                return new ArenaRankingEntry(
                    0,
                    profile.UserName,
                    account?.Profile?.Name ?? profile.RoleName,
                    profile.Score,
                    profile.Wins,
                    profile.Losses,
                    account is null ? 0 : ResolveTotalPower(account),
                    profile.UpdatedAt);
            })
            .OrderByDescending(entry => entry.Score)
            .ThenByDescending(entry => entry.TotalPower)
            .ThenBy(entry => entry.UpdatedAt)
            .Select((entry, index) => entry with { Rank = index + 1 })
            .ToList();

    private ArenaOpponent? CreateOpponent(string userName, int rank)
    {
        PlayerAccount? account = _accounts.GetAccount(userName);
        if (account?.Profile is null)
        {
            return null;
        }

        ArenaProfile profile = EnsureProfile(account);
        List<OwnedMonster> formation = SelectFormation(account);
        OwnedMonster? strongest = formation
            .OrderByDescending(monster => monster.Power)
            .ThenByDescending(monster => monster.Level)
            .FirstOrDefault();

        return new ArenaOpponent(
            account.UserName,
            account.Profile.Name,
            rank,
            profile.Score,
            ResolveTotalPower(account),
            strongest?.Power ?? 0,
            strongest?.Name ?? string.Empty,
            formation);
    }

    private int ResolveRank(string userName)
        => BuildRankings().FirstOrDefault(entry => IsSameUser(entry.UserName, userName))?.Rank ?? 0;

    private static List<OwnedMonster> SelectFormation(PlayerAccount account)
        => account.Monsters
            .OrderByDescending(monster => monster.Power)
            .ThenByDescending(monster => monster.Level)
            .Take(6)
            .ToList();

    private static int ResolveTotalPower(PlayerAccount account)
        => account.Monsters.Sum(monster => monster.Power);

    private static int ResolveScoreDelta(int score, int opponentScore, bool victory)
    {
        int difficultyBonus = Math.Clamp((opponentScore - score) / 25, -8, 12);
        return victory
            ? Math.Max(8, WinBaseScore + difficultyBonus)
            : -Math.Max(5, LoseBaseScore - difficultyBonus / 2);
    }

    private List<PlayerAccount> CreatePracticeOpponents(PlayerAccount player, int count)
    {
        List<OwnedMonster> source = SelectFormation(player);
        if (source.Count == 0)
        {
            return new List<PlayerAccount>();
        }

        List<PlayerAccount> bots = new();
        for (int index = 0; index < count; index++)
        {
            double scale = 0.86 + index * 0.08;
            List<OwnedMonster> monsters = source
                .Select(monster => ClonePracticeMonster(monster, index, scale))
                .ToList();
            bots.Add(new PlayerAccount(
                $"arena-practice-{index + 1}",
                string.Empty,
                new PlayerProfile($"巡场守擂者{index + 1}", DateTime.UtcNow),
                monsters,
                new List<BackpackEquipmentItem>(),
                new List<BackpackConsumableItem>(),
                new List<BackpackFragmentItem>(),
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                player.Level,
                0));
        }

        return bots;
    }

    private static OwnedMonster ClonePracticeMonster(OwnedMonster monster, int seed, double scale)
        => new(
            100000 + seed * 100 + monster.Id,
            monster.TemplateId,
            monster.Name,
            Math.Clamp((int)Math.Round(monster.Level * scale), 1, GameAccountStore.MaxMonsterLevel),
            Math.Clamp(monster.Star, 1, GameAccountStore.MaxMonsterStar),
            monster.Rank,
            monster.Attribute,
            monster.Element,
            monster.Position,
            Math.Max(1, (int)Math.Round(monster.Power * scale)),
            monster.PortraitUrl,
            false,
            monster.Equipment.Select(slot => new MonsterEquipmentSlot(slot.Name, slot.Equipped, slot.IconText, slot.SlotKey, slot.ItemInstanceId)).ToList(),
            monster.Experience,
            Math.Clamp(monster.Stage, 1, GameAccountStore.MaxMonsterStage),
            Math.Clamp((int)Math.Round(monster.DemonEssence * scale), 0, GameAccountStore.MaxDemonEssence));

    private static ArenaOpponent CreatePracticeOpponent(PlayerAccount account, int baseScore, int offset)
    {
        List<OwnedMonster> formation = SelectFormation(account);
        OwnedMonster? strongest = formation
            .OrderByDescending(monster => monster.Power)
            .FirstOrDefault();

        return new ArenaOpponent(
            account.UserName,
            account.Profile?.Name ?? account.UserName,
            999 + offset,
            Math.Max(600, baseScore - 45 + offset * 30),
            ResolveTotalPower(account),
            strongest?.Power ?? 0,
            strongest?.Name ?? string.Empty,
            formation);
    }

    private void LoadProfiles()
    {
        ILiteCollection<ArenaProfileDocument> collection = _database.GetCollection<ArenaProfileDocument>("arena_profiles");
        collection.EnsureIndex(profile => profile.UserName, unique: true);

        foreach (ArenaProfileDocument document in collection.FindAll())
        {
            if (string.IsNullOrWhiteSpace(document.UserName))
            {
                continue;
            }

            ArenaProfile profile = new(
                document.UserName,
                document.RoleName,
                Math.Max(0, document.Score),
                Math.Max(0, document.Wins),
                Math.Max(0, document.Losses),
                Math.Max(0, document.ChallengeCount),
                document.UpdatedAt == default ? DateTimeOffset.UtcNow : document.UpdatedAt);
            _profiles[profile.UserName] = profile;
        }
    }

    private void SaveProfile(ArenaProfile profile)
    {
        _profiles[profile.UserName] = profile;
        ILiteCollection<ArenaProfileDocument> collection = _database.GetCollection<ArenaProfileDocument>("arena_profiles");
        collection.Upsert(new ArenaProfileDocument
        {
            UserName = profile.UserName,
            RoleName = profile.RoleName,
            Score = profile.Score,
            Wins = profile.Wins,
            Losses = profile.Losses,
            ChallengeCount = profile.ChallengeCount,
            UpdatedAt = profile.UpdatedAt,
        });
    }

    private static bool IsSameUser(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
