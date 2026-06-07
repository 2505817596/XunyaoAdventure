using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class GameAccountStore
{
    public const string ExpPotionTemplateId = "small-exp-potion";
    public const string DemonEssenceTemplateId = "demon-essence";
    public const string BreakthroughPillTemplateId = "breakthrough-pill";
    public const string SoulStoneTemplateId = "soul-stone";
    public const int ExpPotionValue = 120;
    public const int MaxMonsterLevel = 100;
    public const int MaxMonsterStar = 5;
    public const int MaxMonsterStage = 5;
    public const int MaxDemonEssence = 20;
    private const string QualityWhite = "白";
    private const string QualityGreen = "绿";
    private const string QualityBlue = "蓝";
    private const string QualityPurple = "紫";
    private const string QualityRed = "红";

    private readonly System.Threading.Lock _gate = new();
    private readonly GameConfigStore _configs;
    private readonly GameDatabase _database;
    private readonly Dictionary<string, PlayerAccount> _accounts = new(StringComparer.OrdinalIgnoreCase);
    private int _nextMonsterId = 1;

    public GameAccountStore(GameConfigStore configs, GameDatabase database)
    {
        _configs = configs;
        _database = database;
        LoadAccounts();
    }

    public GameLoginResult Register(string userName, string password)
    {
        userName = Normalize(userName);
        if (!ValidateCredentials(userName, password, out string message))
        {
            return new GameLoginResult(false, message, null);
        }

        lock (_gate)
        {
            if (_accounts.ContainsKey(userName))
            {
                return new GameLoginResult(false, "账号已存在", null);
            }

            InitialAccountConfig initialAccount = _configs.GetInitialAccountConfig();
            PlayerAccount account = new(
                userName,
                password,
                null,
                new List<OwnedMonster>(),
                new List<BackpackEquipmentItem>(),
                new List<BackpackConsumableItem>(),
                new List<BackpackFragmentItem>(),
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                initialAccount.Level,
                initialAccount.Copper);
            _accounts.Add(userName, account);
            SaveAccount(account);
            return new GameLoginResult(true, "注册成功", account);
        }
    }

    public GameLoginResult Login(string userName, string password)
    {
        userName = Normalize(userName);
        lock (_gate)
        {
            if (!_accounts.TryGetValue(userName, out PlayerAccount? account) || account.Password != password)
            {
                return new GameLoginResult(false, "账号或密码错误", null);
            }

            return new GameLoginResult(true, "登录成功", account);
        }
    }

    public PlayerAccount? GetAccount(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        lock (_gate)
        {
            if (!_accounts.TryGetValue(Normalize(userName), out PlayerAccount? account))
            {
                return null;
            }

            RecalculateAccountDerivedStats(account);
            return account;
        }
    }

    public IReadOnlyList<PlayerAccount> GetAccounts()
    {
        lock (_gate)
        {
            foreach (PlayerAccount account in _accounts.Values)
            {
                RecalculateAccountDerivedStats(account);
            }

            return _accounts.Values
                .OrderBy(account => account.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public OwnedMonster? GetMonster(string? userName, int monsterId)
        => GetAccount(userName)?.Monsters.FirstOrDefault(monster => monster.Id == monsterId);

    public ResolvedEquipmentItem ResolveEquipmentItem(BackpackEquipmentItem item)
    {
        EquipmentTemplateConfig? template = _configs.GetEquipmentTemplates()
            .FirstOrDefault(template => string.Equals(template.TemplateId, item.TemplateId, StringComparison.OrdinalIgnoreCase));

        string quality = string.IsNullOrWhiteSpace(item.QualityOverride)
            ? QualityWhite
            : item.QualityOverride;
        int upgradeLevel = Math.Max(0, item.UpgradeLevel);
        HeroAttributeConfig stats = template is null
            ? new HeroAttributeConfig()
            : ResolveEquipmentStats(template.Stats, QualityWhite, quality);

        return new ResolvedEquipmentItem(
            item.InstanceId,
            item.TemplateId,
            template?.Name ?? "未知装备",
            template?.SlotKey ?? "weapon",
            template?.ImagePath ?? string.Empty,
            CalculateEquipmentPower(stats, quality, upgradeLevel),
            quality,
            BuildEquipmentAttributeText(stats, quality, upgradeLevel),
            stats,
            item.EquippedMonsterId,
            upgradeLevel);
    }

    public IReadOnlyList<RankingEntry> GetRankings(RankingBoardType boardType, int limit = 50)
    {
        lock (_gate)
        {
            IEnumerable<RankingEntry> entries = _accounts.Values
                .Where(account => account.Profile is not null)
                .Select(CreateRankingEntry);

            entries = boardType switch
            {
                RankingBoardType.StrongestMonster => entries
                    .OrderByDescending(entry => entry.StrongestMonsterPower)
                    .ThenByDescending(entry => entry.TotalPower)
                    .ThenBy(entry => entry.CreatedAt),
                RankingBoardType.Level => entries
                    .OrderByDescending(entry => entry.Level)
                    .ThenByDescending(entry => entry.TotalPower)
                    .ThenBy(entry => entry.CreatedAt),
                RankingBoardType.Copper => entries
                    .OrderByDescending(entry => entry.Copper)
                    .ThenByDescending(entry => entry.TotalPower)
                    .ThenBy(entry => entry.CreatedAt),
                _ => entries
                    .OrderByDescending(entry => entry.TotalPower)
                    .ThenByDescending(entry => entry.StrongestMonsterPower)
                    .ThenBy(entry => entry.CreatedAt),
            };

            return entries
                .Take(Math.Max(1, limit))
                .Select((entry, index) => entry with { Rank = index + 1 })
                .ToList();
        }
    }

    public PlayerAccount? CreateCharacter(string userName, string roleName)
    {
        userName = Normalize(userName);
        roleName = Normalize(roleName);
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }

        lock (_gate)
        {
            if (!_accounts.TryGetValue(userName, out PlayerAccount? account))
            {
                return null;
            }

            if (account.Profile is not null)
            {
                return null;
            }

            account.Profile = new PlayerProfile(roleName, DateTime.UtcNow);
            account.Monsters = CreateInitialMonsters();
            account.Backpack = new List<BackpackEquipmentItem>();
            SaveAccount(account);
            return account;
        }
    }

    public GameEquipResult EquipItem(string? userName, int monsterId, int itemInstanceId)
    {
        if (!TryGetAccountForWrite(userName, out PlayerAccount? account, out GameEquipResult? error))
        {
            return error!;
        }

        int monsterIndex = account.Monsters.FindIndex(monster => monster.Id == monsterId);
        if (monsterIndex < 0)
        {
            return new GameEquipResult(false, "没有找到这个妖怪", account);
        }

        int itemIndex = account.Backpack.FindIndex(item => item.InstanceId == itemInstanceId);
        if (itemIndex < 0)
        {
            return new GameEquipResult(false, "背包里没有这件装备", account);
        }

        OwnedMonster monster = account.Monsters[monsterIndex];
        BackpackEquipmentItem item = account.Backpack[itemIndex];
        ResolvedEquipmentItem resolvedItem = ResolveEquipmentItem(item);
        int slotIndex = monster.Equipment.ToList().FindIndex(slot => slot.SlotKey == resolvedItem.SlotKey);
        if (slotIndex < 0)
        {
            return new GameEquipResult(false, "这个妖怪不能穿戴该装备", account);
        }

        if (item.EquippedMonsterId is not null && item.EquippedMonsterId != monsterId)
        {
            return new GameEquipResult(false, "该装备已被其他妖怪穿戴", account);
        }

        List<BackpackEquipmentItem> backpack = account.Backpack;
        List<MonsterEquipmentSlot> slots = monster.Equipment;
        MonsterEquipmentSlot currentSlot = slots[slotIndex];
        if (currentSlot.ItemInstanceId is int currentItemId && currentItemId != itemInstanceId)
        {
            int currentItemIndex = backpack.FindIndex(backpackItem => backpackItem.InstanceId == currentItemId);
            if (currentItemIndex >= 0)
            {
                backpack[currentItemIndex].EquippedMonsterId = null;
            }
        }

        item.EquippedMonsterId = monsterId;
        currentSlot.Name = resolvedItem.Name;
        currentSlot.Equipped = true;
        currentSlot.ItemInstanceId = item.InstanceId;

        monster.Power = CalculateMonsterPower(monster.Level, monster.Star, monster.Rank, monster.Stage, monster.DemonEssence, slots, backpack, ResolveEquipmentItem);

        SaveAccount(account);
        return new GameEquipResult(true, "穿戴成功", account);
    }

    public GameEquipResult UnequipItem(string? userName, int monsterId, string slotKey)
    {
        if (!TryGetAccountForWrite(userName, out PlayerAccount? account, out GameEquipResult? error))
        {
            return error!;
        }

        int monsterIndex = account.Monsters.FindIndex(monster => monster.Id == monsterId);
        if (monsterIndex < 0)
        {
            return new GameEquipResult(false, "没有找到这个妖怪", account);
        }

        OwnedMonster monster = account.Monsters[monsterIndex];
        List<MonsterEquipmentSlot> slots = monster.Equipment;
        int slotIndex = slots.FindIndex(slot => slot.SlotKey == slotKey);
        if (slotIndex < 0)
        {
            return new GameEquipResult(false, "装备栏不存在", account);
        }

        List<BackpackEquipmentItem> backpack = account.Backpack;
        if (slots[slotIndex].ItemInstanceId is int itemInstanceId)
        {
            int itemIndex = backpack.FindIndex(item => item.InstanceId == itemInstanceId);
            if (itemIndex >= 0)
            {
                backpack[itemIndex].EquippedMonsterId = null;
            }
        }

        slots[slotIndex] = CreateEmptySlot(slotKey);
        monster.Power = CalculateMonsterPower(monster.Level, monster.Star, monster.Rank, monster.Stage, monster.DemonEssence, slots, backpack, ResolveEquipmentItem);

        SaveAccount(account);
        return new GameEquipResult(true, "已卸下装备", account);
    }

    public GameEquipResult UpgradeEquipment(string? userName, int itemInstanceId)
    {
        if (!TryGetAccountForWrite(userName, out PlayerAccount? account, out GameEquipResult? error))
        {
            return error!;
        }

        int itemIndex = account.Backpack.FindIndex(item => item.InstanceId == itemInstanceId);
        if (itemIndex < 0)
        {
            return new GameEquipResult(false, "背包里没有这件装备", account);
        }

        BackpackEquipmentItem item = account.Backpack[itemIndex];
        if (item.UpgradeLevel >= account.Level)
        {
            return new GameEquipResult(false, "强化等级不能超过账号等级", account);
        }

        ResolvedEquipmentItem resolvedItem = ResolveEquipmentItem(item);
        int cost = GetEquipmentUpgradeCost(item.UpgradeLevel + 1, resolvedItem.Quality);
        if (account.Copper < cost)
        {
            return new GameEquipResult(false, $"铜钱不足，需要{cost}", account);
        }

        int upgradeLevel = item.UpgradeLevel + 1;
        item.UpgradeLevel = upgradeLevel;

        RefreshMonstersUsingItems(account.Monsters, account.Backpack, itemInstanceId, ResolveEquipmentItem);
        account.Copper -= cost;
        SaveAccount(account);
        return new GameEquipResult(true, $"强化成功，当前{upgradeLevel}", account);
    }

    public GameEquipResult FuseEquipmentByMain(string? userName, int mainItemId, IReadOnlyList<int> materialItemIds)
    {
        if (!TryGetAccountForWrite(userName, out PlayerAccount? account, out GameEquipResult? error))
        {
            return error!;
        }

        BackpackEquipmentItem? mainItem = account.Backpack.FirstOrDefault(item => item.InstanceId == mainItemId);
        if (mainItem is null)
        {
            return new GameEquipResult(false, "主装备不存在", account);
        }

        ResolvedEquipmentItem resolvedMainItem = ResolveEquipmentItem(mainItem);
        string? nextQuality = GetNextQuality(resolvedMainItem.Quality);
        if (nextQuality is null)
        {
            return new GameEquipResult(false, "主装备已经达到最高品质", account);
        }

        int requiredMaterials = GetRequiredFuseMaterialCount(resolvedMainItem.Quality);
        List<int> materialIds = materialItemIds.Distinct().ToList();
        if (materialIds.Count != requiredMaterials)
        {
            return new GameEquipResult(false, $"需要{requiredMaterials}件同阶材料装备", account);
        }

        if (materialIds.Contains(mainItemId))
        {
            return new GameEquipResult(false, "主装备不能作为材料消耗", account);
        }

        List<BackpackEquipmentItem> materials = materialIds
            .Select(id => account.Backpack.FirstOrDefault(item => item.InstanceId == id))
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
        if (materials.Count != requiredMaterials)
        {
            return new GameEquipResult(false, "材料装备选择无效", account);
        }

        if (materials.Any(item => item.EquippedMonsterId is not null))
        {
            return new GameEquipResult(false, "已穿戴装备不能作为材料", account);
        }

        if (materials.Any(item => NormalizeQuality(ResolveEquipmentItem(item).Quality) != NormalizeQuality(resolvedMainItem.Quality)))
        {
            return new GameEquipResult(false, "只能消耗同阶装备作为材料", account);
        }

        account.Backpack.RemoveAll(item => materialIds.Contains(item.InstanceId));
        mainItem.QualityOverride = nextQuality;

        ResolvedEquipmentItem upgradedItem = ResolveEquipmentItem(mainItem);
        RefreshMonstersUsingItems(account.Monsters, account.Backpack, mainItemId, ResolveEquipmentItem);
        SaveAccount(account);
        return new GameEquipResult(true, $"升阶成功，{upgradedItem.Name}提升为{nextQuality}", account);
    }

    public GameLevelUpResult LevelUpMonster(string? userName, int monsterId, int potionCount)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new GameLevelUpResult(false, "请先登录", null, 0, 0, 0);
        }

        potionCount = Math.Max(0, potionCount);
        if (potionCount <= 0)
        {
            return new GameLevelUpResult(false, "药水数量必须大于0", GetAccount(userName), 0, 0, 0);
        }

        lock (_gate)
        {
            if (!_accounts.TryGetValue(Normalize(userName), out PlayerAccount? account))
            {
                return new GameLevelUpResult(false, "账号不存在", null, 0, 0, 0);
            }

            int monsterIndex = account.Monsters.FindIndex(monster => monster.Id == monsterId);
            if (monsterIndex < 0)
            {
                return new GameLevelUpResult(false, "没有找到这个妖怪", account, 0, 0, 0);
            }

            List<BackpackConsumableItem> consumables = account.Consumables;
            int potionIndex = consumables.FindIndex(item => item.EffectType == "MonsterExp");
            OwnedMonster monster = account.Monsters[monsterIndex];
            if (potionIndex < 0 || consumables[potionIndex].Quantity <= 0)
            {
                return new GameLevelUpResult(false, "经验药水不足", account, monster.Level, monster.Experience, 0);
            }

            if (monster.Level >= MaxMonsterLevel)
            {
                return new GameLevelUpResult(false, "已达到等级上限", account, monster.Level, monster.Experience, 0);
            }

            int usableCount = Math.Min(potionCount, consumables[potionIndex].Quantity);
            int oldLevel = monster.Level;
            int level = monster.Level;
            int experience = monster.Experience;
            int consumed = 0;
            for (int i = 0; i < usableCount && level < MaxMonsterLevel; i++)
            {
                experience += consumables[potionIndex].EffectValue;
                consumed++;
                while (level < MaxMonsterLevel && experience >= GetRequiredExperienceForNextLevel(level))
                {
                    experience -= GetRequiredExperienceForNextLevel(level);
                    level++;
                }
            }

            if (consumed <= 0)
            {
                return new GameLevelUpResult(false, "没有消耗药水", account, monster.Level, monster.Experience, 0);
            }

            if (level >= MaxMonsterLevel)
            {
                experience = 0;
            }

            consumables[potionIndex].Quantity -= consumed;
            List<BackpackEquipmentItem> backpack = account.Backpack;
            monster.Level = level;
            monster.Experience = experience;
            monster.Power = CalculateMonsterPower(level, monster.Star, monster.Rank, monster.Stage, monster.DemonEssence, monster.Equipment, backpack, ResolveEquipmentItem);
            SaveAccount(account);

            string message = level > oldLevel ? $"升级成功，当前{level}级" : $"已获得经验，当前{level}级";
            return new GameLevelUpResult(true, message, account, level, experience, consumed);
        }
    }

    public GameMaterialUseResult UseDemonEssence(string? userName, int monsterId, int count)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new GameMaterialUseResult(false, "请先登录", null, 0);
        }

        count = Math.Max(0, count);
        if (count <= 0)
        {
            return new GameMaterialUseResult(false, "妖元数量必须大于0", GetAccount(userName), 0);
        }

        lock (_gate)
        {
            if (!_accounts.TryGetValue(Normalize(userName), out PlayerAccount? account))
            {
                return new GameMaterialUseResult(false, "账号不存在", null, 0);
            }

            int monsterIndex = account.Monsters.FindIndex(monster => monster.Id == monsterId);
            if (monsterIndex < 0)
            {
                return new GameMaterialUseResult(false, "没有找到这个妖怪", account, 0);
            }

            List<BackpackConsumableItem> consumables = account.Consumables;
            int materialIndex = consumables.FindIndex(item => item.EffectType == "DemonEssence");
            OwnedMonster monster = account.Monsters[monsterIndex];
            if (monster.DemonEssence >= MaxDemonEssence)
            {
                return new GameMaterialUseResult(false, "妖元已达到上限", account, 0);
            }

            if (materialIndex < 0 || consumables[materialIndex].Quantity <= 0)
            {
                return new GameMaterialUseResult(false, "妖元不足", account, 0);
            }

            int consumed = Math.Min(count, Math.Min(consumables[materialIndex].Quantity, MaxDemonEssence - monster.DemonEssence));
            int demonEssence = monster.DemonEssence + consumed;
            consumables[materialIndex].Quantity -= consumed;
            List<BackpackEquipmentItem> backpack = account.Backpack;
            monster.DemonEssence = demonEssence;
            monster.Power = CalculateMonsterPower(monster.Level, monster.Star, monster.Rank, monster.Stage, demonEssence, monster.Equipment, backpack, ResolveEquipmentItem);
            SaveAccount(account);

            return new GameMaterialUseResult(true, $"已吸收妖元{consumed}", account, consumed);
        }
    }

    public GameMaterialUseResult BreakthroughMonster(string? userName, int monsterId)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new GameMaterialUseResult(false, "请先登录", null, 0);
        }

        lock (_gate)
        {
            if (!_accounts.TryGetValue(Normalize(userName), out PlayerAccount? account))
            {
                return new GameMaterialUseResult(false, "账号不存在", null, 0);
            }

            int monsterIndex = account.Monsters.FindIndex(monster => monster.Id == monsterId);
            if (monsterIndex < 0)
            {
                return new GameMaterialUseResult(false, "没有找到这个妖怪", account, 0);
            }

            OwnedMonster monster = account.Monsters[monsterIndex];
            if (monster.Stage >= MaxMonsterStage)
            {
                return new GameMaterialUseResult(false, "已达到最高阶", account, 0);
            }

            int requiredEssence = GetRequiredDemonEssenceForStage(monster.Stage);
            if (monster.DemonEssence < requiredEssence)
            {
                return new GameMaterialUseResult(false, $"需要累计妖元{requiredEssence}", account, 0);
            }

            List<BackpackConsumableItem> consumables = account.Consumables;
            int pillIndex = consumables.FindIndex(item => item.EffectType == "BreakthroughPill");
            int requiredPills = GetRequiredBreakthroughPills(monster.Stage);
            if (pillIndex < 0 || consumables[pillIndex].Quantity < requiredPills)
            {
                return new GameMaterialUseResult(false, $"破境丹不足，需要{requiredPills}", account, 0);
            }

            int stage = monster.Stage + 1;
            string rank = ResolveRankName(stage);
            consumables[pillIndex].Quantity -= requiredPills;
            List<BackpackEquipmentItem> backpack = account.Backpack;
            monster.Stage = stage;
            monster.Rank = rank;
            monster.Power = CalculateMonsterPower(monster.Level, monster.Star, rank, stage, monster.DemonEssence, monster.Equipment, backpack, ResolveEquipmentItem);
            SaveAccount(account);

            return new GameMaterialUseResult(true, $"突破成功，当前{stage}阶", account, requiredPills);
        }
    }

    public GameMaterialUseResult StarUpMonster(string? userName, int monsterId)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new GameMaterialUseResult(false, "请先登录", null, 0);
        }

        lock (_gate)
        {
            if (!_accounts.TryGetValue(Normalize(userName), out PlayerAccount? account))
            {
                return new GameMaterialUseResult(false, "账号不存在", null, 0);
            }

            int monsterIndex = account.Monsters.FindIndex(monster => monster.Id == monsterId);
            if (monsterIndex < 0)
            {
                return new GameMaterialUseResult(false, "没有找到这个妖怪", account, 0);
            }

            OwnedMonster monster = account.Monsters[monsterIndex];
            if (monster.Star >= MaxMonsterStar)
            {
                return new GameMaterialUseResult(false, "已达到最高星", account, 0);
            }

            List<BackpackConsumableItem> consumables = account.Consumables;
            int soulStoneIndex = consumables.FindIndex(item => item.EffectType == "SoulStone");
            int requiredSoulStones = GetRequiredSoulStonesForStar(monster.Star);
            if (soulStoneIndex < 0 || consumables[soulStoneIndex].Quantity < requiredSoulStones)
            {
                return new GameMaterialUseResult(false, $"灵魂石不足，需要{requiredSoulStones}", account, 0);
            }

            consumables[soulStoneIndex].Quantity -= requiredSoulStones;
            List<BackpackEquipmentItem> backpack = account.Backpack;
            int star = monster.Star + 1;
            monster.Star = star;
            monster.Power = CalculateMonsterPower(monster.Level, star, monster.Rank, monster.Stage, monster.DemonEssence, monster.Equipment, backpack, ResolveEquipmentItem);
            SaveAccount(account);

            return new GameMaterialUseResult(true, $"升星成功，当前{star}星", account, requiredSoulStones);
        }
    }

    public PlayerAccount? GrantRewards(string? userName, IReadOnlyList<QuestReward> rewards)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        lock (_gate)
        {
            if (!_accounts.TryGetValue(Normalize(userName), out PlayerAccount? account))
            {
                return null;
            }

            ApplyRewards(account, rewards);
            SaveAccount(account);
            return account;
        }
    }

    public bool TrySpendCopper(string? userName, int amount, out PlayerAccount? account, out string message)
    {
        account = null;
        amount = Math.Max(0, amount);
        if (string.IsNullOrWhiteSpace(userName))
        {
            message = "请先登录";
            return false;
        }

        if (amount <= 0)
        {
            message = "消耗数量必须大于0";
            return false;
        }

        lock (_gate)
        {
            if (!_accounts.TryGetValue(Normalize(userName), out account))
            {
                message = "账号不存在";
                return false;
            }

            if (account.Copper < amount)
            {
                message = $"铜钱不足，需要{amount}";
                return false;
            }

            account.Copper -= amount;
            SaveAccount(account);
            message = "扣除成功";
            return true;
        }
    }

    public GachaDrawResult DrawGacha(string? userName, string? poolId, int drawCount)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new GachaDrawResult(false, "请先登录", null, Array.Empty<GachaDrawItem>());
        }

        if (drawCount is not (1 or 10))
        {
            return new GachaDrawResult(false, "只支持1抽或10抽", GetAccount(userName), Array.Empty<GachaDrawItem>());
        }

        GachaPoolConfig? pool = _configs.GetGachaPool(poolId);
        if (pool is null)
        {
            return new GachaDrawResult(false, "奖池不存在", GetAccount(userName), Array.Empty<GachaDrawItem>());
        }

        if (pool.Entries.Count == 0)
        {
            return new GachaDrawResult(false, "奖池没有配置掉落", GetAccount(userName), Array.Empty<GachaDrawItem>());
        }

        lock (_gate)
        {
            if (!_accounts.TryGetValue(Normalize(userName), out PlayerAccount? account))
            {
                return new GachaDrawResult(false, "账号不存在", null, Array.Empty<GachaDrawItem>());
            }

            if (account.Profile is null)
            {
                return new GachaDrawResult(false, "请先创建角色", account, Array.Empty<GachaDrawItem>());
            }

            int totalCost = drawCount;
            BackpackConsumableItem? costItem = account.Consumables.FirstOrDefault(item =>
                string.Equals(item.TemplateId, pool.CostItemTemplateId, StringComparison.OrdinalIgnoreCase));
            if (costItem is null || costItem.Quantity < totalCost)
            {
                string costItemName = ResolveConsumableName(pool.CostItemTemplateId);
                return new GachaDrawResult(false, $"{costItemName}不足，需要{totalCost}", account, Array.Empty<GachaDrawItem>());
            }

            List<GachaEntryConfig> entries = new();
            for (int i = 0; i < drawCount; i++)
            {
                entries.Add(PickGachaEntry(account, pool));
            }

            string? validationError = ValidateGachaEntries(entries);
            if (validationError is not null)
            {
                return new GachaDrawResult(false, validationError, account, Array.Empty<GachaDrawItem>());
            }

            List<GachaDrawItem> items = new();
            foreach (GachaEntryConfig entry in entries)
            {
                if (!ApplyGachaEntry(account, entry, out GachaDrawItem item, out string error))
                {
                    return new GachaDrawResult(false, error, account, items);
                }

                items.Add(item);
            }

            costItem.Quantity -= totalCost;
            SaveAccount(account);
            return new GachaDrawResult(true, "抽取成功", account, items);
        }
    }

    public static int GetRequiredExperienceForNextLevel(int level)
    {
        level = Math.Clamp(level, 1, MaxMonsterLevel);
        return 90 + level * 35;
    }

    public static int GetRequiredDemonEssenceForStage(int stage)
        => Math.Clamp(stage, 1, MaxMonsterStage) * 5;

    public static int GetRequiredBreakthroughPills(int stage)
        => Math.Clamp(stage, 1, MaxMonsterStage - 1);

    public static int GetRequiredSoulStonesForStar(int star)
        => Math.Clamp(star, 1, MaxMonsterStar - 1) switch
        {
            1 => 10,
            2 => 20,
            3 => 40,
            _ => 80,
        };

    public static int GetEquipmentUpgradeCost(int nextLevel, string quality)
        => Math.Max(1, nextLevel) * (NormalizeQuality(quality) switch
        {
            QualityGreen => 140,
            QualityBlue => 220,
            QualityPurple => 360,
            QualityRed => 620,
            _ => 100,
        });

    public static int GetRequiredFuseMaterialCount(string quality)
        => NormalizeQuality(quality) switch
        {
            QualityWhite => 1,
            QualityGreen => 2,
            QualityBlue => 3,
            QualityPurple => 4,
            _ => 0,
        };

    private List<OwnedMonster> CreateInitialMonsters()
    {
        HashSet<string> configuredIds = _configs.GetInitialRoleConfig()
            .MonsterTemplateIds
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<MonsterTemplateConfig> templates = _configs.GetMonsterTemplates()
            .Where(template => configuredIds.Contains(template.TemplateId))
            .ToList();

        return templates
            .Select((template, index) => CreateMonsterFromTemplate(template, index == 0))
            .ToList();
    }

    private OwnedMonster CreateMonsterFromTemplate(MonsterTemplateConfig template, bool isFavorite)
        => new(
            _nextMonsterId++,
            template.TemplateId,
            template.Name,
            template.InitialLevel,
            template.InitialStar,
            ResolveRankName(template.InitialStage),
            template.Attribute,
            template.Element,
            template.Position,
            template.InitialPower,
            template.PortraitUrl,
            isFavorite,
            new[]
            {
                CreateEmptySlot("weapon"),
                CreateEmptySlot("armor"),
                CreateEmptySlot("necklace"),
                CreateEmptySlot("boots"),
                CreateEmptySlot("relic"),
                CreateEmptySlot("soul"),
            },
            0,
            template.InitialStage,
            0);

    private BackpackConsumableItem? CreateConsumableFromTemplate(string templateId, int quantity)
    {
        ConsumableTemplateConfig? template = _configs.GetConsumableTemplates()
            .FirstOrDefault(item => string.Equals(item.TemplateId, templateId, StringComparison.OrdinalIgnoreCase));
        return template is null
            ? null
            : new BackpackConsumableItem(
                template.TemplateId,
                template.Name,
                Math.Max(0, quantity),
                template.EffectType,
                template.EffectValue,
                template.Description);
    }

    private BackpackFragmentItem? CreateFragmentFromTemplate(string templateId, int quantity)
    {
        FragmentTemplateConfig? template = _configs.GetFragmentTemplates()
            .FirstOrDefault(item => string.Equals(item.TemplateId, templateId, StringComparison.OrdinalIgnoreCase));
        return template is null
            ? null
            : new BackpackFragmentItem(
                template.TemplateId,
                template.Name,
                Math.Max(0, quantity),
                template.Quality,
                template.Description);
    }

    private string ResolveConsumableName(string templateId)
        => _configs.GetConsumableTemplates()
            .FirstOrDefault(item => string.Equals(item.TemplateId, templateId, StringComparison.OrdinalIgnoreCase))
            ?.Name
            ?? "抽卡道具";

    private string ResolveConsumableImagePath(string templateId)
        => _configs.GetConsumableTemplates()
            .FirstOrDefault(item => string.Equals(item.TemplateId, templateId, StringComparison.OrdinalIgnoreCase))
            ?.ImagePath
            ?? string.Empty;

    private string ResolveFragmentImagePath(string templateId)
        => _configs.GetFragmentTemplates()
            .FirstOrDefault(item => string.Equals(item.TemplateId, templateId, StringComparison.OrdinalIgnoreCase))
            ?.ImagePath
            ?? string.Empty;

    private bool ApplyGachaEntry(PlayerAccount account, GachaEntryConfig entry, out GachaDrawItem item, out string error)
    {
        item = new GachaDrawItem(entry.Type, entry.TemplateId, entry.Name, entry.ImagePath, entry.Quantity, null);
        error = string.Empty;

        if (entry.Type == "Copper")
        {
            account.Copper += entry.Quantity;
            return true;
        }

        if (entry.Type == "Monster")
        {
            MonsterTemplateConfig? template = _configs.GetMonsterTemplate(entry.TemplateId);
            if (template is null)
            {
                error = $"妖怪模板不存在：{entry.TemplateId}";
                return false;
            }

            OwnedMonster monster = CreateMonsterFromTemplate(template, account.Monsters.Count == 0);
            account.Monsters.Add(monster);
            item = new GachaDrawItem(entry.Type, template.TemplateId, template.Name, template.PortraitUrl, 1, monster.Id);
            return true;
        }

        if (entry.Type == "Consumable")
        {
            int index = account.Consumables.FindIndex(backpackItem =>
                string.Equals(backpackItem.TemplateId, entry.TemplateId, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                account.Consumables[index].Quantity += entry.Quantity;
                BackpackConsumableItem current = account.Consumables[index];
                item = new GachaDrawItem(entry.Type, current.TemplateId, current.Name, ResolveConsumableImagePath(current.TemplateId), entry.Quantity, null);
                return true;
            }

            BackpackConsumableItem? newItem = CreateConsumableFromTemplate(entry.TemplateId, entry.Quantity);
            if (newItem is null)
            {
                error = $"道具模板不存在：{entry.TemplateId}";
                return false;
            }

            account.Consumables.Add(newItem);
            item = new GachaDrawItem(entry.Type, newItem.TemplateId, newItem.Name, ResolveConsumableImagePath(newItem.TemplateId), entry.Quantity, null);
            return true;
        }

        if (entry.Type == "Fragment")
        {
            int index = account.Fragments.FindIndex(backpackItem =>
                string.Equals(backpackItem.TemplateId, entry.TemplateId, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                account.Fragments[index].Quantity += entry.Quantity;
                BackpackFragmentItem current = account.Fragments[index];
                item = new GachaDrawItem(entry.Type, current.TemplateId, current.Name, ResolveFragmentImagePath(current.TemplateId), entry.Quantity, null);
                return true;
            }

            BackpackFragmentItem? newItem = CreateFragmentFromTemplate(entry.TemplateId, entry.Quantity);
            if (newItem is null)
            {
                error = $"碎片模板不存在：{entry.TemplateId}";
                return false;
            }

            account.Fragments.Add(newItem);
            item = new GachaDrawItem(entry.Type, newItem.TemplateId, newItem.Name, ResolveFragmentImagePath(newItem.TemplateId), entry.Quantity, null);
            return true;
        }

        error = $"不支持的掉落类型：{entry.Type}";
        return false;
    }

    private string? ValidateGachaEntries(IReadOnlyList<GachaEntryConfig> entries)
    {
        foreach (GachaEntryConfig entry in entries)
        {
            if (entry.Type == "Monster" && _configs.GetMonsterTemplate(entry.TemplateId) is null)
            {
                return $"妖怪模板不存在：{entry.TemplateId}";
            }

            if (entry.Type == "Consumable"
                && !_configs.GetConsumableTemplates().Any(item =>
                    string.Equals(item.TemplateId, entry.TemplateId, StringComparison.OrdinalIgnoreCase)))
            {
                return $"道具模板不存在：{entry.TemplateId}";
            }

            if (entry.Type == "Fragment"
                && !_configs.GetFragmentTemplates().Any(item =>
                    string.Equals(item.TemplateId, entry.TemplateId, StringComparison.OrdinalIgnoreCase)))
            {
                return $"碎片模板不存在：{entry.TemplateId}";
            }
        }

        return null;
    }

    private static GachaEntryConfig PickGachaEntry(PlayerAccount account, GachaPoolConfig pool)
    {
        string poolId = pool.Id.Trim();
        account.GachaPityCounters.TryGetValue(poolId, out int currentCount);
        int nextCount = currentCount + 1;
        bool hitPity = pool.PityDrawCount > 0 && nextCount >= pool.PityDrawCount;

        if (hitPity)
        {
            List<GachaEntryConfig> pityEntries = pool.Entries
                .Where(entry => entry.Weight <= pool.PityMaxWeight)
                .ToList();
            if (pityEntries.Count > 0)
            {
                account.GachaPityCounters[poolId] = 0;
                return PickGachaEntryEvenly(pityEntries);
            }
        }

        account.GachaPityCounters[poolId] = nextCount;
        return PickGachaEntryByWeight(pool.Entries);
    }

    private static GachaEntryConfig PickGachaEntryEvenly(IReadOnlyList<GachaEntryConfig> entries)
        => entries[Random.Shared.Next(0, entries.Count)];

    private static GachaEntryConfig PickGachaEntryByWeight(IReadOnlyList<GachaEntryConfig> entries)
    {
        int totalWeight = entries.Sum(entry => Math.Max(1, entry.Weight));
        int roll = Random.Shared.Next(1, totalWeight + 1);
        int cursor = 0;
        foreach (GachaEntryConfig entry in entries)
        {
            cursor += Math.Max(1, entry.Weight);
            if (roll <= cursor)
            {
                return entry;
            }
        }

        return entries[^1];
    }

    private BackpackEquipmentItem? CreateEquipmentFromTemplate(int instanceId, string templateId)
    {
        EquipmentTemplateConfig? template = _configs.GetEquipmentTemplates()
            .FirstOrDefault(item => string.Equals(item.TemplateId, templateId, StringComparison.OrdinalIgnoreCase));
        if (template is null)
        {
            return null;
        }

        return new BackpackEquipmentItem(
            instanceId,
            template.TemplateId,
            string.Empty,
            null,
            template.UpgradeLevel);
    }

    private static MonsterEquipmentSlot CreateEmptySlot(string slotKey)
        => new(ResolveSlotName(slotKey), false, slotKey, null);

    private static string ResolveSlotName(string slotKey)
        => slotKey switch
        {
            "weapon" => "武器",
            "armor" => "护甲",
            "necklace" => "饰品",
            "boots" => "鞋子",
            "relic" => "法器",
            "soul" => "命星",
            _ => "装备",
        };
    private static string ResolveRankName(int stage)
        => Math.Clamp(stage, 1, MaxMonsterStage) switch
        {
            1 => QualityWhite,
            2 => QualityGreen,
            3 => QualityBlue,
            4 => QualityPurple,
            _ => QualityRed,
        };

    public static int CalculateEquipmentPower(HeroAttributeConfig stats, string quality, int upgradeLevel)
    {
        double basePower =
            stats.Strength * 4.2 +
            stats.Intelligence * 4.2 +
            stats.Agility * 4.2 +
            stats.MaxHp * 0.18 +
            stats.PhysicalAttack * 3.8 +
            stats.MagicPower * 3.8 +
            stats.PhysicalArmor * 3.2 +
            stats.MagicResist * 3.2 +
            stats.PhysicalCrit * 2.5 +
            stats.HpRegen * 2.0 +
            stats.EnergyRegen * 120.0 +
            stats.InterruptThreshold * 1.8;
        double qualityMultiplier = GetEquipmentQualityPowerMultiplier(quality);
        return Math.Max(1, (int)Math.Round(basePower * qualityMultiplier + Math.Max(0, upgradeLevel) * 8 * qualityMultiplier));
    }

    public static string BuildEquipmentAttributeText(HeroAttributeConfig stats, string quality, int upgradeLevel)
    {
        List<string> lines = new();
        AddStat(lines, "力量", stats.Strength);
        AddStat(lines, "智力", stats.Intelligence);
        AddStat(lines, "敏捷", stats.Agility);
        AddStat(lines, "生命", stats.MaxHp);
        AddStat(lines, "物理攻击", stats.PhysicalAttack);
        AddStat(lines, "魔法强度", stats.MagicPower);
        AddStat(lines, "物理护甲", stats.PhysicalArmor);
        AddStat(lines, "魔法抗性", stats.MagicResist);
        AddStat(lines, "物理暴击", stats.PhysicalCrit);
        AddStat(lines, "生命回复", stats.HpRegen);
        AddStat(lines, "能量回复", stats.EnergyRegen);
        AddStat(lines, "打断阈值", stats.InterruptThreshold);
        if (upgradeLevel > 0)
        {
            lines.Add($"强化战力 +{Math.Round(upgradeLevel * 8 * GetEquipmentQualityPowerMultiplier(quality)):0}");
        }

        return lines.Count == 0 ? "无属性" : string.Join(" / ", lines);
    }
    private static HeroAttributeConfig ResolveEquipmentStats(HeroAttributeConfig stats, string templateQuality, string currentQuality)
    {
        double templateMultiplier = GetEquipmentQualityStatMultiplier(templateQuality);
        double currentMultiplier = GetEquipmentQualityStatMultiplier(currentQuality);
        double scale = templateMultiplier <= 0 ? currentMultiplier : currentMultiplier / templateMultiplier;

        return new HeroAttributeConfig
        {
            Strength = RoundStat(stats.Strength * scale),
            Intelligence = RoundStat(stats.Intelligence * scale),
            Agility = RoundStat(stats.Agility * scale),
            MaxHp = RoundStat(stats.MaxHp * scale),
            PhysicalAttack = RoundStat(stats.PhysicalAttack * scale),
            MagicPower = RoundStat(stats.MagicPower * scale),
            PhysicalArmor = RoundStat(stats.PhysicalArmor * scale),
            MagicResist = RoundStat(stats.MagicResist * scale),
            PhysicalCrit = RoundStat(stats.PhysicalCrit * scale),
            HpRegen = RoundStat(stats.HpRegen * scale),
            EnergyRegen = RoundStat(stats.EnergyRegen * scale),
            InterruptThreshold = RoundStat(stats.InterruptThreshold * scale),
        };
    }

    private static double GetEquipmentQualityPowerMultiplier(string quality)
        => NormalizeQuality(quality) switch
        {
            QualityGreen => 1.18,
            QualityBlue => 1.42,
            QualityPurple => 1.78,
            QualityRed => 2.25,
            _ => 1.00,
        };

    private static double GetEquipmentQualityStatMultiplier(string quality)
        => NormalizeQuality(quality) switch
        {
            QualityGreen => 1.22,
            QualityBlue => 1.55,
            QualityPurple => 2.05,
            QualityRed => 2.80,
            _ => 1.00,
        };

    private static double RoundStat(double value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static HeroAttributeConfig CloneAttributeConfig(HeroAttributeConfig stats)
        => new()
        {
            Strength = stats.Strength,
            Intelligence = stats.Intelligence,
            Agility = stats.Agility,
            MaxHp = stats.MaxHp,
            PhysicalAttack = stats.PhysicalAttack,
            MagicPower = stats.MagicPower,
            PhysicalArmor = stats.PhysicalArmor,
            MagicResist = stats.MagicResist,
            PhysicalCrit = stats.PhysicalCrit,
            HpRegen = stats.HpRegen,
            EnergyRegen = stats.EnergyRegen,
            InterruptThreshold = stats.InterruptThreshold,
        };

    private static void AddStat(List<string> lines, string name, double value)
    {
        if (value <= 0)
        {
            return;
        }

        lines.Add($"{name} +{value:0.##}");
    }

    private static string? GetNextQuality(string quality)
        => NormalizeQuality(quality) switch
        {
            QualityWhite => QualityGreen,
            QualityGreen => QualityBlue,
            QualityBlue => QualityPurple,
            QualityPurple => QualityRed,
            _ => null,
        };

    private static string NormalizeQuality(string quality)
    {
        if (quality.Contains('红')) return QualityRed;
        if (quality.Contains('紫')) return QualityPurple;
        if (quality.Contains('蓝')) return QualityBlue;
        if (quality.Contains('绿')) return QualityGreen;
        if (quality.Contains('白')) return QualityWhite;
        return QualityWhite;
    }

    private static void RefreshMonstersUsingItems(
        List<OwnedMonster> monsters,
        IReadOnlyList<BackpackEquipmentItem> backpack,
        int itemInstanceId,
        Func<BackpackEquipmentItem, ResolvedEquipmentItem> resolveEquipment)
    {
        foreach (OwnedMonster monster in monsters)
        {
            List<MonsterEquipmentSlot> slots = monster.Equipment;
            int slotIndex = slots.FindIndex(slot => slot.ItemInstanceId == itemInstanceId);
            if (slotIndex < 0)
            {
                continue;
            }

            BackpackEquipmentItem item = backpack.First(backpackItem => backpackItem.InstanceId == itemInstanceId);
            slots[slotIndex].Name = resolveEquipment(item).Name;
            monster.Power = CalculateMonsterPower(monster.Level, monster.Star, monster.Rank, monster.Stage, monster.DemonEssence, slots, backpack, resolveEquipment);
        }
    }

    private static int CalculateMonsterPower(
        int level,
        int star,
        string rank,
        int stage,
        int demonEssence,
        IReadOnlyList<MonsterEquipmentSlot> slots,
        IReadOnlyList<BackpackEquipmentItem> backpack,
        Func<BackpackEquipmentItem, ResolvedEquipmentItem> resolveEquipment)
    {
        int rankPower = rank switch
        {
            QualityWhite => 0,
            QualityGreen => 20,
            QualityBlue => 48,
            QualityPurple => 90,
            QualityRed => 140,
            _ => 0,
        };
        int equippedPower = slots
            .Select(slot => slot.ItemInstanceId)
            .Where(itemId => itemId is not null)
            .Select(itemId => backpack.FirstOrDefault(item => item.InstanceId == itemId))
            .Where(item => item is not null)
            .Sum(item => resolveEquipment(item!).Power);
        int stagePower = (Math.Clamp(stage, 1, MaxMonsterStage) - 1) * 45;
        int starPower = (Math.Clamp(star, 1, MaxMonsterStar) - 1) * 36;
        int essencePower = Math.Clamp(demonEssence, 0, MaxDemonEssence) * 8;

        return 80 + level * 12 + star * 16 + rankPower + starPower + stagePower + essencePower + equippedPower;
    }

    private bool TryGetAccountForWrite(string? userName, out PlayerAccount account, out GameEquipResult? error)
    {
        account = null!;
        error = null;
        if (string.IsNullOrWhiteSpace(userName))
        {
            error = new GameEquipResult(false, "请先登录", null);
            return false;
        }

        if (!_accounts.TryGetValue(Normalize(userName), out PlayerAccount? found))
        {
            error = new GameEquipResult(false, "账号不存在", null);
            return false;
        }

        account = found;
        return true;
    }

    private void LoadAccounts()
    {
        ILiteCollection<PlayerAccountDocument> collection = _database.GetCollection<PlayerAccountDocument>("player_accounts");

        foreach (PlayerAccountDocument document in collection.FindAll())
        {
            if (string.IsNullOrWhiteSpace(document.UserName))
            {
                continue;
            }

            PlayerAccount account = ToAccount(document);
            _accounts[Normalize(document.UserName)] = account;
        }

        int maxMonsterId = _accounts.Values
            .SelectMany(account => account.Monsters)
            .Select(monster => monster.Id)
            .DefaultIfEmpty(0)
            .Max();
        _nextMonsterId = maxMonsterId + 1;
    }

    private void SaveAccount(PlayerAccount account)
    {
        RecalculateAccountDerivedStats(account);
        _accounts[account.UserName] = account;
        SaveAccountDocument(account);
    }

    private void RecalculateAccountDerivedStats(PlayerAccount account)
    {
        foreach (OwnedMonster monster in account.Monsters)
        {
            monster.Power = CalculateMonsterPower(
                monster.Level,
                monster.Star,
                monster.Rank,
                monster.Stage,
                monster.DemonEssence,
                monster.Equipment,
                account.Backpack,
                ResolveEquipmentItem);
        }
    }

    internal void SaveAccountDocument(PlayerAccount account)
    {
        ILiteCollection<PlayerAccountDocument> collection = _database.GetCollection<PlayerAccountDocument>("player_accounts");
        collection.Upsert(new PlayerAccountDocument
        {
            UserName = account.UserName,
            Password = account.Password,
            Profile = account.Profile is null
                ? null
                : new PlayerProfileDocument
                {
                    Name = account.Profile.Name,
                    CreatedAt = account.Profile.CreatedAt,
                },
            Monsters = account.Monsters.Select(ToDocument).ToList(),
            Backpack = account.Backpack.Select(ToDocument).ToList(),
            Consumables = account.Consumables.Select(ToDocument).ToList(),
            Fragments = account.Fragments.Select(ToDocument).ToList(),
            GachaPityCounters = new Dictionary<string, int>(account.GachaPityCounters, StringComparer.OrdinalIgnoreCase),
            Level = account.Level,
            Copper = account.Copper,
        });
    }

    private void ApplyRewards(PlayerAccount account, IReadOnlyList<QuestReward> rewards)
    {
        foreach (QuestReward reward in rewards)
        {
            if (reward.Type == QuestRewardType.Copper)
            {
                account.Copper += reward.Quantity;
                continue;
            }

            if (reward.Type == QuestRewardType.Consumable && !string.IsNullOrWhiteSpace(reward.TemplateId))
            {
                int index = account.Consumables.FindIndex(item => item.TemplateId == reward.TemplateId);
                if (index >= 0)
                {
                    account.Consumables[index].Quantity += reward.Quantity;
                }
                else if (CreateConsumableFromTemplate(reward.TemplateId, reward.Quantity) is BackpackConsumableItem newItem)
                {
                    account.Consumables.Add(newItem);
                }
            }

            if (reward.Type == QuestRewardType.Equipment && !string.IsNullOrWhiteSpace(reward.TemplateId))
            {
                int nextInstanceId = GetNextEquipmentInstanceId(account);
                for (int i = 0; i < reward.Quantity; i++)
                {
                    BackpackEquipmentItem? newItem = CreateEquipmentFromTemplate(nextInstanceId + i, reward.TemplateId);
                    if (newItem is not null)
                    {
                        account.Backpack.Add(newItem);
                    }
                }
            }
        }
    }

    private static int GetNextEquipmentInstanceId(PlayerAccount account)
        => account.Backpack
            .Select(item => item.InstanceId)
            .DefaultIfEmpty(0)
            .Max() + 1;

    private static PlayerAccount ToAccount(PlayerAccountDocument document)
        => new(
            document.UserName,
            document.Password,
            document.Profile is null ? null : new PlayerProfile(document.Profile.Name, document.Profile.CreatedAt),
            document.Monsters.Select(ToMonster).ToList(),
            document.Backpack.Select(ToBackpackEquipment).ToList(),
            document.Consumables.Select(ToConsumable).ToList(),
            document.Fragments.Select(ToFragment).ToList(),
            new Dictionary<string, int>(document.GachaPityCounters, StringComparer.OrdinalIgnoreCase),
            document.Level,
            document.Copper);

    private static OwnedMonster ToMonster(OwnedMonsterDocument document)
        => new(
            document.Id,
            document.TemplateId,
            document.Name,
            document.Level,
            document.Star,
            document.Rank,
            document.Attribute,
            document.Element,
            document.Position,
            document.Power,
            document.PortraitUrl,
            document.IsFavorite,
            document.Equipment.Select(ToSlot).ToList(),
            document.Experience,
            document.Stage,
            document.DemonEssence);

    private static MonsterEquipmentSlot ToSlot(MonsterEquipmentSlotDocument document)
        => new(document.Name, document.Equipped, document.SlotKey, document.ItemInstanceId);

    private static BackpackEquipmentItem ToBackpackEquipment(BackpackEquipmentItemDocument document)
        => new(
            document.InstanceId,
            document.TemplateId,
            document.QualityOverride,
            document.EquippedMonsterId,
            document.UpgradeLevel);

    private static BackpackConsumableItem ToConsumable(BackpackConsumableItemDocument document)
        => new(
            document.TemplateId,
            document.Name,
            document.Quantity,
            document.EffectType,
            document.EffectValue,
            document.Description);

    private static BackpackFragmentItem ToFragment(BackpackFragmentItemDocument document)
        => new(
            document.TemplateId,
            document.Name,
            document.Quantity,
            document.Quality,
            document.Description);

    private static OwnedMonsterDocument ToDocument(OwnedMonster monster)
        => new()
        {
            Id = monster.Id,
            TemplateId = monster.TemplateId,
            Name = monster.Name,
            Level = monster.Level,
            Star = monster.Star,
            Rank = monster.Rank,
            Attribute = monster.Attribute,
            Element = monster.Element,
            Position = monster.Position,
            Power = monster.Power,
            PortraitUrl = monster.PortraitUrl,
            IsFavorite = monster.IsFavorite,
            Equipment = monster.Equipment.Select(ToDocument).ToList(),
            Experience = monster.Experience,
            Stage = monster.Stage,
            DemonEssence = monster.DemonEssence,
        };

    private static MonsterEquipmentSlotDocument ToDocument(MonsterEquipmentSlot slot)
        => new()
        {
            Name = slot.Name,
            Equipped = slot.Equipped,
            SlotKey = slot.SlotKey,
            ItemInstanceId = slot.ItemInstanceId,
        };

    private static BackpackEquipmentItemDocument ToDocument(BackpackEquipmentItem item)
        => new()
        {
            InstanceId = item.InstanceId,
            TemplateId = item.TemplateId,
            QualityOverride = item.QualityOverride,
            EquippedMonsterId = item.EquippedMonsterId,
            UpgradeLevel = item.UpgradeLevel,
        };

    private static BackpackConsumableItemDocument ToDocument(BackpackConsumableItem item)
        => new()
        {
            TemplateId = item.TemplateId,
            Name = item.Name,
            Quantity = item.Quantity,
            EffectType = item.EffectType,
            EffectValue = item.EffectValue,
            Description = item.Description,
        };

    private static BackpackFragmentItemDocument ToDocument(BackpackFragmentItem item)
        => new()
        {
            TemplateId = item.TemplateId,
            Name = item.Name,
            Quantity = item.Quantity,
            Quality = item.Quality,
            Description = item.Description,
        };

    private static string Normalize(string value)
        => value.Trim();

    private static bool ValidateCredentials(string userName, string password, out string message)
    {
        if (userName.Length < 3)
        {
            message = "账号至少3个字符";
            return false;
        }

        if (password.Length < 3)
        {
            message = "密码至少3个字符";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static RankingEntry CreateRankingEntry(PlayerAccount account)
    {
        List<OwnedMonster> monsters = account.Monsters.ToList();
        OwnedMonster? strongestMonster = monsters
            .OrderByDescending(monster => monster.Power)
            .ThenByDescending(monster => monster.Level)
            .FirstOrDefault();

        return new RankingEntry(
            0,
            account.UserName,
            account.Profile?.Name ?? account.UserName,
            account.Level,
            account.Copper,
            monsters.Sum(monster => monster.Power),
            strongestMonster?.Power ?? 0,
            strongestMonster?.Name ?? string.Empty,
            account.Profile?.CreatedAt ?? DateTime.MaxValue);
    }
}
