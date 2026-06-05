using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace XunyaoAdventure.Game;

public sealed class GameConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    private readonly object _gate = new();
    private readonly string _configDirectory;
    private readonly string _monstersPath;
    private readonly string _campaignPath;
    private readonly string _itemsPath;
    private readonly string _gameplayPath;
    private readonly string _questsPath;
    private readonly string _gachaPath;
    private MonsterConfigFile _monsterConfig = new();
    private CampaignConfigFile _campaignConfig = new();
    private ItemConfigFile _itemConfig = new();
    private GameplayConfigFile _gameplayConfig = new();
    private QuestConfigFile _questConfig = new();
    private GachaConfigFile _gachaConfig = new();

    public GameConfigStore(IWebHostEnvironment environment)
    {
        _configDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "Configs");
        _monstersPath = Path.Combine(_configDirectory, "monsters.json");
        _campaignPath = Path.Combine(_configDirectory, "campaign.json");
        _itemsPath = Path.Combine(_configDirectory, "items.json");
        _gameplayPath = Path.Combine(_configDirectory, "gameplay.json");
        _questsPath = Path.Combine(_configDirectory, "quests.json");
        _gachaPath = Path.Combine(_configDirectory, "gacha.json");

        Directory.CreateDirectory(_configDirectory);
        EnsureConfigFile(_monstersPath, new MonsterConfigFile());
        EnsureConfigFile(_campaignPath, new CampaignConfigFile());
        EnsureConfigFile(_itemsPath, new ItemConfigFile());
        EnsureConfigFile(_gameplayPath, new GameplayConfigFile());
        EnsureConfigFile(_questsPath, new QuestConfigFile());
        EnsureConfigFile(_gachaPath, new GachaConfigFile());

        ReloadMonsters();
        ReloadCampaign();
        ReloadItems();
        ReloadGameplay();
        ReloadQuests();
        ReloadGacha();
    }

    public IReadOnlyList<MonsterTemplateConfig> GetMonsterTemplates()
    {
        lock (_gate)
        {
            return _monsterConfig.Monsters.Select(CloneTemplate).ToList();
        }
    }

    public MonsterTemplateConfig? GetMonsterTemplate(string? templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        lock (_gate)
        {
            MonsterTemplateConfig? template = _monsterConfig.Monsters.FirstOrDefault(item =>
                string.Equals(item.TemplateId, templateId.Trim(), StringComparison.OrdinalIgnoreCase));
            return template is null ? null : CloneTemplate(template);
        }
    }

    public ConfigSaveResult SaveMonsterTemplates(IReadOnlyList<MonsterTemplateConfig> templates)
    {
        List<MonsterTemplateConfig> normalized = templates.Select(CloneTemplate).ToList();
        string? error = ValidateMonsterTemplates(normalized);
        if (error is not null)
        {
            return new ConfigSaveResult(false, error);
        }

        MonsterConfigFile config = new()
        {
            Version = 1,
            Monsters = normalized,
        };

        lock (_gate)
        {
            WriteConfig(_monstersPath, config);
            _monsterConfig = NormalizeMonsterConfig(config);
        }

        return new ConfigSaveResult(true, "淇濆瓨鎴愬姛");
    }

    public ConfigSaveResult SaveMonsterTemplate(MonsterTemplateConfig template, string? originalTemplateId = null)
    {
        MonsterTemplateConfig normalized = CloneTemplate(template);
        lock (_gate)
        {
            List<MonsterTemplateConfig> templates = _monsterConfig.Monsters.Select(CloneTemplate).ToList();
            int index = templates.FindIndex(item =>
                string.Equals(item.TemplateId, originalTemplateId ?? normalized.TemplateId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                templates.Add(normalized);
            }
            else
            {
                templates[index] = normalized;
            }

            return SaveMonsterTemplates(templates);
        }
    }

    public InitialRoleConfig GetInitialRoleConfig()
    {
        lock (_gate)
        {
            return CloneInitialRole(_gameplayConfig.InitialRole);
        }
    }

    public InitialAccountConfig GetInitialAccountConfig()
    {
        lock (_gate)
        {
            return CloneInitialAccount(_gameplayConfig.InitialAccount);
        }
    }

    public GuildConfig GetGuildConfig()
    {
        lock (_gate)
        {
            return CloneGuildConfig(_gameplayConfig.Guild);
        }
    }

    public ConfigSaveResult SaveInitialRoleConfig(InitialRoleConfig initialRole)
        => SaveGameplayConfig(initialRole, GetInitialAccountConfig(), GetGuildConfig());

    public ConfigSaveResult SaveGameplayConfig(InitialRoleConfig initialRole, InitialAccountConfig initialAccount)
        => SaveGameplayConfig(initialRole, initialAccount, GetGuildConfig());

    public ConfigSaveResult SaveGameplayConfig(
        InitialRoleConfig initialRole,
        InitialAccountConfig initialAccount,
        GuildConfig guild)
    {
        GameplayConfigFile config = new()
        {
            Version = 1,
            InitialRole = CloneInitialRole(initialRole),
            InitialAccount = CloneInitialAccount(initialAccount),
            Guild = CloneGuildConfig(guild),
        };

        string? error = ValidateGameplayConfig(config);
        if (error is not null)
        {
            return new ConfigSaveResult(false, error);
        }

        lock (_gate)
        {
            WriteConfig(_gameplayPath, config);
            _gameplayConfig = NormalizeGameplayConfig(config);
        }

        return new ConfigSaveResult(true, "淇濆瓨鎴愬姛");
    }

    public IReadOnlyList<QuestDefinitionConfig> GetQuestDefinitions()
    {
        lock (_gate)
        {
            return _questConfig.Quests.Select(CloneQuest).ToList();
        }
    }

    public ConfigSaveResult SaveQuestDefinitions(IReadOnlyList<QuestDefinitionConfig> quests)
    {
        QuestConfigFile config = new()
        {
            Version = 1,
            Quests = quests.Select(CloneQuest).ToList(),
        };

        string? error = ValidateQuestConfig(config);
        if (error is not null)
        {
            return new ConfigSaveResult(false, error);
        }

        lock (_gate)
        {
            WriteConfig(_questsPath, config);
            _questConfig = NormalizeQuestConfig(config);
        }

        return new ConfigSaveResult(true, "淇濆瓨鎴愬姛");
    }

    public IReadOnlyList<GachaPoolConfig> GetGachaPools()
    {
        lock (_gate)
        {
            return _gachaConfig.Pools.Select(CloneGachaPool).ToList();
        }
    }

    public GachaPoolConfig? GetGachaPool(string? poolId)
    {
        if (string.IsNullOrWhiteSpace(poolId))
        {
            return null;
        }

        lock (_gate)
        {
            GachaPoolConfig? pool = _gachaConfig.Pools.FirstOrDefault(item =>
                string.Equals(item.Id, poolId.Trim(), StringComparison.OrdinalIgnoreCase));
            return pool is null ? null : CloneGachaPool(pool);
        }
    }

    public ConfigSaveResult SaveGachaPools(IReadOnlyList<GachaPoolConfig> pools)
    {
        GachaConfigFile config = new()
        {
            Version = 1,
            Pools = pools.Select(CloneGachaPool).ToList(),
        };

        string? error = ValidateGachaConfig(config);
        if (error is not null)
        {
            return new ConfigSaveResult(false, error);
        }

        lock (_gate)
        {
            WriteConfig(_gachaPath, config);
            _gachaConfig = NormalizeGachaConfig(config);
        }

        return new ConfigSaveResult(true, "淇濆瓨鎴愬姛");
    }

    public IReadOnlyList<ConsumableTemplateConfig> GetConsumableTemplates()
    {
        lock (_gate)
        {
            return _itemConfig.Consumables.Select(CloneConsumable).ToList();
        }
    }

    public IReadOnlyList<EquipmentTemplateConfig> GetEquipmentTemplates()
    {
        lock (_gate)
        {
            return _itemConfig.Equipment.Select(CloneEquipment).ToList();
        }
    }

    public IReadOnlyList<FragmentTemplateConfig> GetFragmentTemplates()
    {
        lock (_gate)
        {
            return _itemConfig.Fragments.Select(CloneFragment).ToList();
        }
    }

    public ConfigSaveResult SaveItemConfig(
        IReadOnlyList<ConsumableTemplateConfig> consumables,
        IReadOnlyList<EquipmentTemplateConfig> equipment,
        IReadOnlyList<FragmentTemplateConfig> fragments)
    {
        ItemConfigFile config = new()
        {
            Version = 1,
            Consumables = consumables.Select(CloneConsumable).ToList(),
            Equipment = equipment.Select(CloneEquipment).ToList(),
            Fragments = fragments.Select(CloneFragment).ToList(),
        };

        string? error = ValidateItemConfig(config);
        if (error is not null)
        {
            return new ConfigSaveResult(false, error);
        }

        lock (_gate)
        {
            WriteConfig(_itemsPath, config);
            _itemConfig = NormalizeItemConfig(config);
        }

        return new ConfigSaveResult(true, "淇濆瓨鎴愬姛");
    }

    public IReadOnlyList<CampaignChapterConfig> GetCampaignChapters()
    {
        lock (_gate)
        {
            return _campaignConfig.Chapters.Select(CloneChapter).ToList();
        }
    }

    public IReadOnlyList<CampaignStageConfig> GetCampaignStages()
    {
        lock (_gate)
        {
            return _campaignConfig.Stages.Select(CloneStage).ToList();
        }
    }

    public CampaignStageConfig? GetCampaignStage(string? stageId)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            return null;
        }

        lock (_gate)
        {
            CampaignStageConfig? stage = _campaignConfig.Stages.FirstOrDefault(item =>
                string.Equals(item.Id, stageId.Trim(), StringComparison.OrdinalIgnoreCase));
            return stage is null ? null : CloneStage(stage);
        }
    }

    public ConfigSaveResult SaveCampaignConfig(
        IReadOnlyList<CampaignChapterConfig> chapters,
        IReadOnlyList<CampaignStageConfig> stages)
    {
        CampaignConfigFile config = new()
        {
            Version = 1,
            Chapters = chapters.Select(CloneChapter).ToList(),
            Stages = stages.Select(CloneStage).ToList(),
        };

        string? error = ValidateCampaignConfig(config);
        if (error is not null)
        {
            return new ConfigSaveResult(false, error);
        }

        lock (_gate)
        {
            WriteConfig(_campaignPath, config);
            _campaignConfig = NormalizeCampaignConfig(config);
        }

        return new ConfigSaveResult(true, "淇濆瓨鎴愬姛");
    }

    private static void EnsureConfigFile<T>(string path, T emptyConfig)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, JsonSerializer.Serialize(emptyConfig, JsonOptions));
        }
    }

    private static void WriteConfig<T>(string path, T config)
        => File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions));

    private void ReloadMonsters()
    {
        MonsterConfigFile? config = JsonSerializer.Deserialize<MonsterConfigFile>(File.ReadAllText(_monstersPath), JsonOptions);
        _monsterConfig = NormalizeMonsterConfig(config ?? new MonsterConfigFile());
    }

    private void ReloadCampaign()
    {
        CampaignConfigFile? config = JsonSerializer.Deserialize<CampaignConfigFile>(File.ReadAllText(_campaignPath), JsonOptions);
        _campaignConfig = NormalizeCampaignConfig(config ?? new CampaignConfigFile());
    }

    private void ReloadItems()
    {
        ItemConfigFile? config = JsonSerializer.Deserialize<ItemConfigFile>(File.ReadAllText(_itemsPath), JsonOptions);
        _itemConfig = NormalizeItemConfig(config ?? new ItemConfigFile());
    }

    private void ReloadGameplay()
    {
        GameplayConfigFile? config = JsonSerializer.Deserialize<GameplayConfigFile>(File.ReadAllText(_gameplayPath), JsonOptions);
        _gameplayConfig = NormalizeGameplayConfig(config ?? new GameplayConfigFile());
    }

    private void ReloadQuests()
    {
        QuestConfigFile? config = JsonSerializer.Deserialize<QuestConfigFile>(File.ReadAllText(_questsPath), JsonOptions);
        _questConfig = NormalizeQuestConfig(config ?? new QuestConfigFile());
    }

    private void ReloadGacha()
    {
        GachaConfigFile? config = JsonSerializer.Deserialize<GachaConfigFile>(File.ReadAllText(_gachaPath), JsonOptions);
        _gachaConfig = NormalizeGachaConfig(config ?? new GachaConfigFile());
    }

    private static MonsterConfigFile NormalizeMonsterConfig(MonsterConfigFile config)
        => new()
        {
            Version = config.Version <= 0 ? 1 : config.Version,
            Monsters = config.Monsters.Select(CloneTemplate).ToList(),
        };

    private static CampaignConfigFile NormalizeCampaignConfig(CampaignConfigFile config)
        => new()
        {
            Version = config.Version <= 0 ? 1 : config.Version,
            Chapters = config.Chapters.Select(CloneChapter).ToList(),
            Stages = config.Stages.Select(CloneStage).ToList(),
        };

    private static ItemConfigFile NormalizeItemConfig(ItemConfigFile config)
        => new()
        {
            Version = config.Version <= 0 ? 1 : config.Version,
            Consumables = config.Consumables.Select(CloneConsumable).ToList(),
            Equipment = config.Equipment.Select(CloneEquipment).ToList(),
            Fragments = config.Fragments.Select(CloneFragment).ToList(),
        };

    private static GameplayConfigFile NormalizeGameplayConfig(GameplayConfigFile config)
        => new()
        {
            Version = config.Version <= 0 ? 1 : config.Version,
            InitialRole = CloneInitialRole(config.InitialRole),
            InitialAccount = CloneInitialAccount(config.InitialAccount),
            Guild = CloneGuildConfig(config.Guild),
        };

    private static QuestConfigFile NormalizeQuestConfig(QuestConfigFile config)
        => new()
        {
            Version = config.Version <= 0 ? 1 : config.Version,
            Quests = config.Quests.Select(CloneQuest).ToList(),
        };

    private static GachaConfigFile NormalizeGachaConfig(GachaConfigFile config)
        => new()
        {
            Version = config.Version <= 0 ? 1 : config.Version,
            Pools = config.Pools.Select(CloneGachaPool).ToList(),
        };

    private static MonsterTemplateConfig CloneTemplate(MonsterTemplateConfig template)
        => new()
        {
            TemplateId = template.TemplateId.Trim(),
            Name = template.Name.Trim(),
            Attribute = template.Attribute.Trim(),
            Element = template.Element.Trim(),
            Position = template.Position.Trim(),
            InitialLevel = Math.Clamp(template.InitialLevel, 1, GameAccountStore.MaxMonsterLevel),
            InitialStar = Math.Clamp(template.InitialStar, 1, GameAccountStore.MaxMonsterStar),
            InitialStage = Math.Clamp(template.InitialStage, 1, GameAccountStore.MaxMonsterStage),
            InitialPower = Math.Max(1, template.InitialPower),
            UnitAssetId = template.UnitAssetId.Trim(),
            PortraitUrl = template.PortraitUrl.Trim(),
            BaseStrength = Math.Max(0, template.BaseStrength),
            BaseIntelligence = Math.Max(0, template.BaseIntelligence),
            BaseAgility = Math.Max(0, template.BaseAgility),
            StrengthGrowth = Math.Max(0, template.StrengthGrowth),
            IntelligenceGrowth = Math.Max(0, template.IntelligenceGrowth),
            AgilityGrowth = Math.Max(0, template.AgilityGrowth),
            BaseMaxHp = Math.Max(1, template.BaseMaxHp),
            BasePhysicalAttack = Math.Max(1, template.BasePhysicalAttack),
            BaseMagicPower = Math.Max(0, template.BaseMagicPower),
            BasePhysicalArmor = Math.Max(0, template.BasePhysicalArmor),
            BaseMagicResist = Math.Max(0, template.BaseMagicResist),
            BasePhysicalCrit = Math.Max(0, template.BasePhysicalCrit),
            BaseHpRegen = Math.Max(0, template.BaseHpRegen),
            BaseEnergyRegen = Math.Max(0, template.BaseEnergyRegen),
            BaseInterruptThreshold = Math.Max(0, template.BaseInterruptThreshold),
            MoveSpeed = Math.Max(0.1, template.MoveSpeed),
            AttackRange = Math.Max(0.1, template.AttackRange),
            AttackInterval = Math.Max(0.1, template.AttackInterval),
        };

    private static InitialRoleConfig CloneInitialRole(InitialRoleConfig? config)
        => new()
        {
            MonsterTemplateIds = (config?.MonsterTemplateIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };

    private static InitialAccountConfig CloneInitialAccount(InitialAccountConfig? config)
    {
        config ??= new InitialAccountConfig();
        return new InitialAccountConfig
        {
            Level = Math.Max(1, config.Level),
            Copper = Math.Max(0, config.Copper),
            DefaultStageId = config.DefaultStageId.Trim(),
            FormationSize = Math.Max(1, config.FormationSize),
        };
    }

    private static GuildConfig CloneGuildConfig(GuildConfig? config)
    {
        config ??= new GuildConfig();
        int maxLevel = Math.Clamp(config.MaxLevel, 1, 100);
        Dictionary<int, int> configuredRequirements = config.LevelRequirements
            .Where(requirement => requirement.Level >= 2 && requirement.Level <= maxLevel)
            .GroupBy(requirement => requirement.Level)
            .ToDictionary(
                group => group.Key,
                group => Math.Max(1, group.Last().RequiredTotalContribution));

        List<GuildLevelRequirementConfig> requirements = new();
        int previousRequirement = 0;
        for (int level = 2; level <= maxLevel; level++)
        {
            int fallback = (level - 1) * 1000;
            int required = configuredRequirements.TryGetValue(level, out int configured)
                ? configured
                : fallback;
            required = Math.Max(previousRequirement + 1, required);
            requirements.Add(new GuildLevelRequirementConfig
            {
                Level = level,
                RequiredTotalContribution = required,
            });
            previousRequirement = required;
        }

        return new GuildConfig
        {
            MaxLevel = maxLevel,
            LevelRequirements = requirements,
        };
    }

    private static QuestDefinitionConfig CloneQuest(QuestDefinitionConfig quest)
        => new()
        {
            Id = quest.Id.Trim(),
            Category = quest.Category.Trim(),
            Title = quest.Title.Trim(),
            Description = quest.Description.Trim(),
            EventType = quest.EventType.Trim(),
            RequiredCount = Math.Max(1, quest.RequiredCount),
            PreviousQuestId = quest.PreviousQuestId.Trim(),
            Rewards = quest.Rewards.Select(CloneQuestReward).ToList(),
        };

    private static QuestRewardConfig CloneQuestReward(QuestRewardConfig reward)
    {
        string type = NormalizeRewardType(reward.Type);
        return new QuestRewardConfig
        {
            Type = type,
            Name = reward.Name.Trim(),
            Quantity = reward.Quantity,
            TemplateId = type == "Copper" ? string.Empty : reward.TemplateId.Trim(),
        };
    }

    private static CampaignChapterConfig CloneChapter(CampaignChapterConfig chapter)
        => new()
        {
            Id = chapter.Id.Trim(),
            Name = chapter.Name.Trim(),
            Description = chapter.Description.Trim(),
            Order = Math.Max(1, chapter.Order),
            Unlocked = chapter.Unlocked,
        };

    private static CampaignStageConfig CloneStage(CampaignStageConfig stage)
        => new()
        {
            Id = stage.Id.Trim(),
            ChapterId = stage.ChapterId.Trim(),
            Code = stage.Code.Trim(),
            Name = stage.Name.Trim(),
            Description = stage.Description.Trim(),
            Order = Math.Max(1, stage.Order),
            RecommendedPower = Math.Max(1, stage.RecommendedPower),
            Difficulty = Math.Clamp(stage.Difficulty, 1, 10),
            BaseUnlocked = stage.BaseUnlocked,
            Rewards = CloneRewards(stage.Rewards),
            FirstClearRewards = CloneRewards(stage.FirstClearRewards),
            Waves = stage.Waves.Take(3).Select(CloneWave).ToList(),
        };

    private static List<CampaignRewardConfig> CloneRewards(IReadOnlyList<CampaignRewardConfig>? rewards)
        => (rewards ?? Array.Empty<CampaignRewardConfig>())
            .Select(CloneReward)
            .ToList();

    private static CampaignRewardConfig CloneReward(CampaignRewardConfig reward)
    {
        string type = NormalizeRewardType(reward.Type);
        return new CampaignRewardConfig
        {
            Type = type,
            Name = reward.Name.Trim(),
            Quantity = reward.Quantity,
            TemplateId = type == "Copper" ? string.Empty : reward.TemplateId.Trim(),
        };
    }

    private static CampaignEnemyWaveConfig CloneWave(CampaignEnemyWaveConfig wave)
        => new()
        {
            Enemies = wave.Enemies.Take(6).Select(CloneEnemy).ToList(),
        };

    private static CampaignEnemyConfig CloneEnemy(CampaignEnemyConfig enemy)
        => new()
        {
            TemplateId = enemy.TemplateId.Trim(),
            Name = enemy.Name.Trim(),
            Level = Math.Max(1, enemy.Level),
            Star = Math.Clamp(enemy.Star, 1, GameAccountStore.MaxMonsterStar),
            Stage = Math.Clamp(enemy.Stage, 1, GameAccountStore.MaxMonsterStage),
            X = enemy.X,
            Y = enemy.Y,
        };

    private static ConsumableTemplateConfig CloneConsumable(ConsumableTemplateConfig item)
        => new()
        {
            TemplateId = item.TemplateId.Trim(),
            Name = item.Name.Trim(),
            IconText = item.IconText.Trim(),
            EffectType = item.EffectType.Trim(),
            EffectValue = Math.Max(0, item.EffectValue),
            Description = item.Description.Trim(),
        };

    private static EquipmentTemplateConfig CloneEquipment(EquipmentTemplateConfig item)
        => new()
        {
            TemplateId = item.TemplateId.Trim(),
            Name = item.Name.Trim(),
            SlotKey = item.SlotKey.Trim(),
            IconText = item.IconText.Trim(),
            Quality = item.Quality.Trim(),
            Stats = CloneAttributeConfig(item.Stats),
            UpgradeLevel = Math.Max(0, item.UpgradeLevel),
        };

    private static FragmentTemplateConfig CloneFragment(FragmentTemplateConfig item)
        => new()
        {
            TemplateId = item.TemplateId.Trim(),
            Name = item.Name.Trim(),
            IconText = item.IconText.Trim(),
            Quality = item.Quality.Trim(),
            Description = item.Description.Trim(),
        };

    private static GachaPoolConfig CloneGachaPool(GachaPoolConfig pool)
        => new()
        {
            Id = pool.Id.Trim(),
            Name = pool.Name.Trim(),
            Description = pool.Description.Trim(),
            CostItemTemplateId = pool.CostItemTemplateId.Trim(),
            PityDrawCount = Math.Max(0, pool.PityDrawCount),
            PityMaxWeight = Math.Max(0, pool.PityMaxWeight),
            Entries = pool.Entries.Select(CloneGachaEntry).ToList(),
        };

    private static GachaEntryConfig CloneGachaEntry(GachaEntryConfig entry)
    {
        string type = NormalizeGachaEntryType(entry.Type);
        return new GachaEntryConfig
        {
            Type = type,
            TemplateId = type == "Copper" ? string.Empty : entry.TemplateId.Trim(),
            Name = entry.Name.Trim(),
            IconText = entry.IconText.Trim(),
            Quantity = Math.Max(1, entry.Quantity),
            Weight = Math.Max(1, entry.Weight),
        };
    }

    private static HeroAttributeConfig CloneAttributeConfig(HeroAttributeConfig? stats)
    {
        stats ??= new HeroAttributeConfig();
        return new HeroAttributeConfig
        {
            Strength = Math.Max(0, stats.Strength),
            Intelligence = Math.Max(0, stats.Intelligence),
            Agility = Math.Max(0, stats.Agility),
            MaxHp = Math.Max(0, stats.MaxHp),
            PhysicalAttack = Math.Max(0, stats.PhysicalAttack),
            MagicPower = Math.Max(0, stats.MagicPower),
            PhysicalArmor = Math.Max(0, stats.PhysicalArmor),
            MagicResist = Math.Max(0, stats.MagicResist),
            PhysicalCrit = Math.Max(0, stats.PhysicalCrit),
            HpRegen = Math.Max(0, stats.HpRegen),
            EnergyRegen = Math.Max(0, stats.EnergyRegen),
            InterruptThreshold = Math.Max(0, stats.InterruptThreshold),
        };
    }

    private static string? ValidateMonsterTemplates(IReadOnlyList<MonsterTemplateConfig> templates)
    {
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        foreach (MonsterTemplateConfig template in templates)
        {
            if (string.IsNullOrWhiteSpace(template.TemplateId))
            {
                return "妖怪模板ID不能为空";
            }

            if (!ids.Add(template.TemplateId.Trim()))
            {
                return $"妖怪模板ID重复：{template.TemplateId}";
            }

            if (string.IsNullOrWhiteSpace(template.Name))
            {
                return $"{template.TemplateId} 的名称不能为空";
            }

            if (string.IsNullOrWhiteSpace(template.UnitAssetId))
            {
                return $"{template.Name} 的资源ID不能为空";
            }
        }

        return null;
    }

    private string? ValidateGameplayConfig(GameplayConfigFile config)
    {
        foreach (string templateId in config.InitialRole.MonsterTemplateIds)
        {
            if (!_monsterConfig.Monsters.Any(monster =>
                    string.Equals(monster.TemplateId, templateId, StringComparison.OrdinalIgnoreCase)))
            {
                return $"初始妖怪模板不存在：{templateId}";
            }
        }

        if (config.InitialAccount.Level <= 0)
        {
            return "初始账号等级必须大于0";
        }

        if (config.InitialAccount.Copper < 0)
        {
            return "初始铜钱不能小于0";
        }

        if (config.InitialAccount.FormationSize <= 0)
        {
            return "默认阵容数量必须大于0";
        }

        if (config.Guild.MaxLevel <= 0)
        {
            return "公会等级上限必须大于0";
        }

        int previousRequirement = 0;
        foreach (GuildLevelRequirementConfig requirement in config.Guild.LevelRequirements.OrderBy(requirement => requirement.Level))
        {
            if (requirement.Level < 2 || requirement.Level > config.Guild.MaxLevel)
            {
                return "公会等级贡献配置超出等级上限";
            }

            if (requirement.RequiredTotalContribution <= 0)
            {
                return $"{requirement.Level}级所需贡献必须大于0";
            }

            if (requirement.RequiredTotalContribution <= previousRequirement)
            {
                return "公会等级所需贡献必须逐级递增";
            }

            previousRequirement = requirement.RequiredTotalContribution;
        }

        if (!string.IsNullOrWhiteSpace(config.InitialAccount.DefaultStageId)
            && !_campaignConfig.Stages.Any(stage =>
                string.Equals(stage.Id, config.InitialAccount.DefaultStageId, StringComparison.OrdinalIgnoreCase)))
        {
            return $"默认关卡不存在：{config.InitialAccount.DefaultStageId}";
        }

        return null;
    }

    private static string? ValidateItemConfig(ItemConfigFile config)
    {
        string? duplicate = FindDuplicateId(config.Consumables.Select(item => item.TemplateId));
        if (duplicate is not null)
        {
            return $"道具模板ID重复：{duplicate}";
        }

        duplicate = FindDuplicateId(config.Equipment.Select(item => item.TemplateId));
        if (duplicate is not null)
        {
            return $"装备模板ID重复：{duplicate}";
        }

        duplicate = FindDuplicateId(config.Fragments.Select(item => item.TemplateId));
        if (duplicate is not null)
        {
            return $"碎片模板ID重复：{duplicate}";
        }

        if (config.Consumables.Any(item => string.IsNullOrWhiteSpace(item.TemplateId) || string.IsNullOrWhiteSpace(item.Name)))
        {
            return "道具模板ID和名称不能为空";
        }

        if (config.Equipment.Any(item => string.IsNullOrWhiteSpace(item.TemplateId) || string.IsNullOrWhiteSpace(item.Name)))
        {
            return "装备模板ID和名称不能为空";
        }

        if (config.Fragments.Any(item => string.IsNullOrWhiteSpace(item.TemplateId) || string.IsNullOrWhiteSpace(item.Name)))
        {
            return "碎片模板ID和名称不能为空";
        }

        return null;
    }

    private string? ValidateQuestConfig(QuestConfigFile config)
    {
        string? duplicate = FindDuplicateId(config.Quests.Select(quest => quest.Id));
        if (duplicate is not null)
        {
            return $"任务ID重复：{duplicate}";
        }

        HashSet<string> questIds = config.Quests.Select(quest => quest.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (QuestDefinitionConfig quest in config.Quests)
        {
            if (string.IsNullOrWhiteSpace(quest.Id) || string.IsNullOrWhiteSpace(quest.Title))
            {
                return "任务ID和标题不能为空";
            }

            if (!Enum.TryParse(quest.Category, ignoreCase: true, out QuestCategory _))
            {
                return $"{quest.Id} 的任务分类无效";
            }

            if (!Enum.TryParse(quest.EventType, ignoreCase: true, out QuestEventType _))
            {
                return $"{quest.Id} 的任务事件无效";
            }

            if (quest.RequiredCount <= 0)
            {
                return $"{quest.Id} 的完成次数必须大于0";
            }

            if (!string.IsNullOrWhiteSpace(quest.PreviousQuestId) && !questIds.Contains(quest.PreviousQuestId))
            {
                return $"{quest.Id} 的前置任务不存在：{quest.PreviousQuestId}";
            }

            foreach (QuestRewardConfig reward in quest.Rewards)
            {
                string type = NormalizeRewardType(reward.Type);
                if (type is not ("Copper" or "Consumable"))
                {
                    return $"{quest.Id} 奖励类型只支持 Copper/Consumable";
                }

                if (string.IsNullOrWhiteSpace(reward.Name))
                {
                    return $"{quest.Id} 奖励名称不能为空";
                }

                if (reward.Quantity <= 0)
                {
                    return $"{quest.Id} 奖励数量必须大于0";
                }

                if (type == "Consumable"
                    && !_itemConfig.Consumables.Any(item =>
                        string.Equals(item.TemplateId, reward.TemplateId, StringComparison.OrdinalIgnoreCase)))
                {
                    return $"{quest.Id} 奖励道具不存在：{reward.TemplateId}";
                }
            }
        }

        return null;
    }

    private string? ValidateCampaignConfig(CampaignConfigFile config)
    {
        HashSet<string> chapterIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (CampaignChapterConfig chapter in config.Chapters)
        {
            if (string.IsNullOrWhiteSpace(chapter.Id) || string.IsNullOrWhiteSpace(chapter.Name))
            {
                return "章节ID和名称不能为空";
            }

            if (!chapterIds.Add(chapter.Id))
            {
                return $"章节ID重复：{chapter.Id}";
            }
        }

        HashSet<string> stageIds = new(StringComparer.OrdinalIgnoreCase);
        foreach (CampaignStageConfig stage in config.Stages)
        {
            if (string.IsNullOrWhiteSpace(stage.Id) || string.IsNullOrWhiteSpace(stage.Code) || string.IsNullOrWhiteSpace(stage.Name))
            {
                return "关卡ID、编号和名称不能为空";
            }

            if (!stageIds.Add(stage.Id))
            {
                return $"关卡ID重复：{stage.Id}";
            }

            if (!chapterIds.Contains(stage.ChapterId))
            {
                return $"{stage.Id} 的章节ID不存在：{stage.ChapterId}";
            }

            if (stage.Waves.Count > 3)
            {
                return $"{stage.Code} 最多配置3波敌人";
            }

            string? rewardError = ValidateCampaignRewards(stage);
            if (rewardError is not null)
            {
                return rewardError;
            }

            for (int waveIndex = 0; waveIndex < stage.Waves.Count; waveIndex++)
            {
                CampaignEnemyWaveConfig wave = stage.Waves[waveIndex];
                if (wave.Enemies.Count > 6)
                {
                    return $"{stage.Code} 第{waveIndex + 1}波最多配置6个敌人";
                }

                foreach (CampaignEnemyConfig enemy in wave.Enemies)
                {
                    if (string.IsNullOrWhiteSpace(enemy.TemplateId))
                    {
                        return $"{stage.Code} 第{waveIndex + 1}波妖怪模板ID不能为空";
                    }

                    if (!_monsterConfig.Monsters.Any(monster =>
                            string.Equals(monster.TemplateId, enemy.TemplateId, StringComparison.OrdinalIgnoreCase)))
                    {
                        return $"{stage.Code} 第{waveIndex + 1}波模板不存在：{enemy.TemplateId}";
                    }
                }
            }
        }

        return null;
    }

    private string? ValidateCampaignRewards(CampaignStageConfig stage)
    {
        foreach (CampaignRewardConfig reward in stage.Rewards.Concat(stage.FirstClearRewards))
        {
            string type = NormalizeRewardType(reward.Type);
            if (type is not ("Copper" or "Consumable"))
            {
                return $"{stage.Code} 奖励类型只支持 Copper/Consumable";
            }

            if (string.IsNullOrWhiteSpace(reward.Name))
            {
                return $"{stage.Code} 奖励名称不能为空";
            }

            if (reward.Quantity <= 0)
            {
                return $"{stage.Code} 奖励数量必须大于0";
            }

            if (type == "Consumable"
                && !_itemConfig.Consumables.Any(item =>
                    string.Equals(item.TemplateId, reward.TemplateId, StringComparison.OrdinalIgnoreCase)))
            {
                return $"{stage.Code} 奖励道具不存在：{reward.TemplateId}";
            }
        }

        return null;
    }

    private string? ValidateGachaConfig(GachaConfigFile config)
    {
        string? duplicate = FindDuplicateId(config.Pools.Select(pool => pool.Id));
        if (duplicate is not null)
        {
            return $"奖池ID重复：{duplicate}";
        }

        foreach (GachaPoolConfig pool in config.Pools)
        {
            if (string.IsNullOrWhiteSpace(pool.Id) || string.IsNullOrWhiteSpace(pool.Name))
            {
                return "奖池ID和名称不能为空";
            }

            if (string.IsNullOrWhiteSpace(pool.CostItemTemplateId))
            {
                return $"{pool.Name} 的消耗道具不能为空";
            }

            if (!_itemConfig.Consumables.Any(item =>
                string.Equals(item.TemplateId, pool.CostItemTemplateId, StringComparison.OrdinalIgnoreCase)))
            {
                return $"{pool.Name} 的消耗道具不存在：{pool.CostItemTemplateId}";
            }

            if (pool.Entries.Count == 0)
            {
                return $"{pool.Name} 至少需要一个掉落项";
            }

            if (pool.PityDrawCount > 0)
            {
                if (pool.PityMaxWeight <= 0)
                {
                    return $"{pool.Name} 开启保底时必须配置过滤权重";
                }

                if (!pool.Entries.Any(entry => entry.Weight <= pool.PityMaxWeight))
                {
                    return $"{pool.Name} 的保底过滤后没有可抽取掉落项";
                }
            }

            foreach (GachaEntryConfig entry in pool.Entries)
            {
                string type = NormalizeGachaEntryType(entry.Type);
                if (type is not ("Monster" or "Consumable" or "Fragment" or "Copper"))
                {
                    return $"{pool.Name} 的掉落类型只支持 Monster/Consumable/Fragment/Copper";
                }

                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    return $"{pool.Name} 的掉落名称不能为空";
                }

                if (entry.Quantity <= 0)
                {
                    return $"{pool.Name} 的掉落数量必须大于0";
                }

                if (entry.Weight <= 0)
                {
                    return $"{pool.Name} 的掉落权重必须大于0";
                }

                if (type == "Monster"
                    && !_monsterConfig.Monsters.Any(item =>
                        string.Equals(item.TemplateId, entry.TemplateId, StringComparison.OrdinalIgnoreCase)))
                {
                    return $"{pool.Name} 的妖怪模板不存在：{entry.TemplateId}";
                }

                if (type == "Consumable"
                    && !_itemConfig.Consumables.Any(item =>
                        string.Equals(item.TemplateId, entry.TemplateId, StringComparison.OrdinalIgnoreCase)))
                {
                    return $"{pool.Name} 的道具模板不存在：{entry.TemplateId}";
                }

                if (type == "Fragment"
                    && !_itemConfig.Fragments.Any(item =>
                        string.Equals(item.TemplateId, entry.TemplateId, StringComparison.OrdinalIgnoreCase)))
                {
                    return $"{pool.Name} 的碎片模板不存在：{entry.TemplateId}";
                }
            }
        }

        return null;
    }

    private static string NormalizeRewardType(string? type)
    {
        if (string.Equals(type, "Copper", StringComparison.OrdinalIgnoreCase))
        {
            return "Copper";
        }

        if (string.Equals(type, "Consumable", StringComparison.OrdinalIgnoreCase))
        {
            return "Consumable";
        }

        return string.Empty;
    }

    private static string NormalizeGachaEntryType(string? type)
    {
        if (string.Equals(type, "Monster", StringComparison.OrdinalIgnoreCase))
        {
            return "Monster";
        }

        if (string.Equals(type, "Consumable", StringComparison.OrdinalIgnoreCase))
        {
            return "Consumable";
        }

        if (string.Equals(type, "Fragment", StringComparison.OrdinalIgnoreCase))
        {
            return "Fragment";
        }

        if (string.Equals(type, "Copper", StringComparison.OrdinalIgnoreCase))
        {
            return "Copper";
        }

        return string.Empty;
    }

    private static string? FindDuplicateId(IEnumerable<string> ids)
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string id in ids.Select(id => id.Trim()))
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (!seen.Add(id))
            {
                return id;
            }
        }

        return null;
    }
}
