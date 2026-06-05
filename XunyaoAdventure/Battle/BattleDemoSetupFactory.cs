using LeanClr.Battle;
using LeanClr.Mathematics;
using XunyaoAdventure.Game;

namespace XunyaoAdventure.Battle;

internal static class BattleDemoSetupFactory
{
    private static GameConfigStore? _configs;

    private static readonly (double X, double Y)[] PlayerFormationSlots =
    {
        (0.92, -0.30),
        (0.78, 0.10),
        (0.56, -0.30),
        (0.42, 0.10),
        (0.24, -0.30),
        (0.10, 0.10),
    };

    private static readonly (double X, double Y)[] EnemyFormationSlots =
    {
        (5.06, -0.30),
        (5.20, 0.10),
        (5.42, -0.30),
        (5.56, 0.10),
        (5.74, -0.30),
        (5.88, 0.10),
    };

    public static void Initialize(GameConfigStore configs)
    {
        _configs = configs;
    }

    private static readonly HeroGrowthProfile[] PlayerProfiles =
    {
        CreateProfile("front-guard", BattlePrimaryAttribute.Strength, 26, 14, 18, 2.9, 1.4, 1.9, 2.2, 1.05, 32, 1.00),
        CreateProfile("warrior", BattlePrimaryAttribute.Strength, 24, 13, 20, 2.6, 1.3, 2.1, 2.0, 1.00, 33, 0.95),
        CreateProfile("spirit-mage", BattlePrimaryAttribute.Intelligence, 15, 28, 17, 1.4, 3.1, 1.6, 1.8, 3.40, 34, 0.90),
        CreateProfile("healer", BattlePrimaryAttribute.Intelligence, 16, 27, 16, 1.5, 2.9, 1.5, 1.6, 3.70, 30, 1.05),
        CreateProfile("assassin", BattlePrimaryAttribute.Agility, 18, 14, 29, 1.8, 1.3, 3.1, 1.45, 3.80, 36, 0.90),
        CreateProfile("archer", BattlePrimaryAttribute.Agility, 17, 15, 28, 1.7, 1.4, 3.0, 1.48, 3.50, 44, 0.85),
    };

    public static byte[] CreateSetupBytes()
        => BattleSetupWire.EncodeSetup(CreateSetup());

    public static byte[] CreateSetupBytes(IEnumerable<OwnedMonster> selectedMonsters)
        => BattleSetupWire.EncodeSetup(CreateSetup(selectedMonsters, null));

    public static byte[] CreateSetupBytes(IEnumerable<OwnedMonster> selectedMonsters, CampaignStageSelection? stage)
        => BattleSetupWire.EncodeSetup(CreateSetup(selectedMonsters, stage));

    public static byte[] CreateArenaSetupBytes(IEnumerable<OwnedMonster> selectedMonsters, IEnumerable<OwnedMonster> opponentMonsters)
        => BattleSetupWire.EncodeSetup(CreateArenaSetup(selectedMonsters, opponentMonsters));

    internal static HeroFinalStats CalculateMonsterStats(OwnedMonster monster, int formationIndex = 0)
        => CalculatePlayerMonsterStats(monster, formationIndex);

    private static BattleEncounterSetup CreateSetup()
    {
        BattleEncounterSetup setup = new();
        AddPlayer(setup, 0, 0.32, -0.28, HeroRank.Purple, 4);
        AddPlayer(setup, 1, 0.1, -0.3, HeroRank.Purple, 4);
        AddPlayer(setup, 2, 0.62, -0.28, HeroRank.Blue, 3);
        AddPlayer(setup, 3, 0.4, -0.3, HeroRank.Blue, 3);
        AddPlayer(setup, 4, 0.92, -0.28, HeroRank.Purple, 4);
        AddPlayer(setup, 5, 0.7, -0.3, HeroRank.Purple, 4);

        AddDemoEnemyWaves(setup);
        return setup;
    }

    private static BattleEncounterSetup CreateSetup(IEnumerable<OwnedMonster> selectedMonsters, CampaignStageSelection? stage)
    {
        BattleEncounterSetup setup = new();
        List<OwnedMonster> monsters = selectedMonsters.Take(PlayerFormationSlots.Length).ToList();
        if (monsters.Count == 0)
        {
            throw new InvalidOperationException("Battle setup requires at least one selected monster.");
        }

        for (int index = 0; index < monsters.Count; index++)
        {
            (double x, double y) = PlayerFormationSlots[index];
            AddPlayer(setup, monsters[index], index, x, y);
        }

        AddDemoEnemyWaves(setup, stage);
        return setup;
    }

    private static BattleEncounterSetup CreateArenaSetup(IEnumerable<OwnedMonster> selectedMonsters, IEnumerable<OwnedMonster> opponentMonsters)
    {
        BattleEncounterSetup setup = new();
        List<OwnedMonster> monsters = selectedMonsters.Take(PlayerFormationSlots.Length).ToList();
        List<OwnedMonster> opponents = opponentMonsters.Take(EnemyFormationSlots.Length).ToList();
        if (monsters.Count == 0 || opponents.Count == 0)
        {
            throw new InvalidOperationException("Arena battle setup requires both player and opponent formations.");
        }

        for (int index = 0; index < monsters.Count; index++)
        {
            (double x, double y) = PlayerFormationSlots[index];
            AddPlayer(setup, monsters[index], index, x, y);
        }

        AddArenaEnemyWave(setup, opponents);
        return setup;
    }

    private static void AddPlayer(
        BattleEncounterSetup setup,
        int profileIndex,
        double x,
        double y,
        HeroRank rank,
        int star)
    {
        HeroGrowthProfile profile = PlayerProfiles[profileIndex];
        HeroFinalStats stats = HeroAttributeCalculator.Calculate(
            profile,
            new HeroProgressionState(
                Level: 35,
                Star: star,
                Rank: rank,
                RankProgressionStats: CreateRankProgressionStats(profile.PrimaryAttribute, rank),
                EquippedStats: CreateEquippedStats(profile.PrimaryAttribute)));

        if (!setup.TryAddPlayerUnit(stats.ToBattleSpawn(x, y, ResolveSkillProfileId(profileIndex))))
        {
            throw new InvalidOperationException("Failed to add player unit.");
        }
    }

    private static void AddPlayer(
        BattleEncounterSetup setup,
        OwnedMonster monster,
        int formationIndex,
        double x,
        double y)
    {
        HeroFinalStats stats = CalculatePlayerMonsterStats(monster, formationIndex);

        if (!setup.TryAddPlayerUnit(stats.ToBattleSpawn(x, y, ResolveSkillProfileId(stats.PrimaryAttribute, monster.Position, formationIndex))))
        {
            throw new InvalidOperationException($"Failed to add selected monster {monster.Id}.");
        }
    }

    private static HeroFinalStats CalculatePlayerMonsterStats(OwnedMonster monster, int formationIndex)
    {
        BattlePrimaryAttribute primaryAttribute = ResolvePrimaryAttribute(monster.Attribute);
        HeroRank rank = ResolveRank(monster.Rank);
        HeroGrowthProfile profile = CreateMonsterProfile(monster, primaryAttribute, formationIndex);
        double equipmentScale = ResolveCurrentEquipmentScale(monster);

        return HeroAttributeCalculator.Calculate(
            profile,
            new HeroProgressionState(
                Level: Math.Max(1, monster.Level),
                Star: Math.Clamp(monster.Star, 1, 5),
                Rank: rank,
                RankProgressionStats: CreateRankProgressionStats(primaryAttribute, rank) + CreateDemonEssenceStats(primaryAttribute, monster.DemonEssence, monster.Stage),
                EquippedStats: CreateEquippedStats(primaryAttribute) * equipmentScale));
    }

    private static void AddDemoEnemyWaves(BattleEncounterSetup setup, CampaignStageSelection? stage = null)
    {
        CampaignStageConfig? stageConfig = _configs?.GetCampaignStage(stage?.StageId);
        if (stageConfig is not null && stageConfig.Waves.Count > 0)
        {
            foreach (CampaignEnemyWaveConfig wave in stageConfig.Waves)
            {
                AddEnemyWave(setup, wave.Enemies.Select(CreateEnemy));
            }

            return;
        }

        throw new InvalidOperationException($"Campaign stage '{stage?.StageId}' has no configured enemy waves.");
    }

    private static void AddEnemyWave(BattleEncounterSetup setup, IEnumerable<BattleUnitSpawn> units)
    {
        BattleWaveSetup wave = setup.CreateEnemyWave();
        foreach (BattleUnitSpawn unit in units)
        {
            if (!wave.TryAddUnit(unit))
            {
                throw new InvalidOperationException("Failed to add enemy unit.");
            }
        }
    }

    private static void AddArenaEnemyWave(BattleEncounterSetup setup, IReadOnlyList<OwnedMonster> opponents)
    {
        BattleWaveSetup wave = setup.CreateEnemyWave();
        for (int index = 0; index < opponents.Count; index++)
        {
            (double x, double y) = EnemyFormationSlots[index];
            OwnedMonster monster = opponents[index];
            HeroFinalStats stats = CalculatePlayerMonsterStats(monster, index);
            int skillProfileId = ResolveSkillProfileId(stats.PrimaryAttribute, monster.Position, index);
            if (!wave.TryAddUnit(stats.ToBattleSpawn(x, y, skillProfileId)))
            {
                throw new InvalidOperationException($"Failed to add arena opponent monster {monster.Id}.");
            }
        }
    }

    private static BattleUnitSpawn CreateEnemy(CampaignEnemyConfig enemy)
    {
        int skillProfileId = BattleSkillProfiles.PlainDamageTest;
        MonsterTemplateConfig template = _configs?.GetMonsterTemplate(enemy.TemplateId)
            ?? throw new InvalidOperationException($"Enemy monster template '{enemy.TemplateId}' was not found.");
        BattlePrimaryAttribute primaryAttribute = ResolvePrimaryAttribute(template.Attribute);
        HeroGrowthProfile profile = CreateProfile(
            $"enemy-{enemy.TemplateId}",
            primaryAttribute,
            skillProfileId,
            template,
            1.0);
        HeroFinalStats stats = HeroAttributeCalculator.Calculate(
            profile,
            new HeroProgressionState(
                Level: enemy.Level,
                Star: enemy.Star,
                Rank: ResolveRank(ResolveStageRank(enemy.Stage)),
                RankProgressionStats: default,
                EquippedStats: default));

        return stats.ToBattleSpawn(enemy.X, enemy.Y, skillProfileId);
    }

    private static HeroGrowthProfile CreateProfile(
        string heroId,
        BattlePrimaryAttribute primaryAttribute,
        double baseStrength,
        double baseIntelligence,
        double baseAgility,
        double strengthGrowth,
        double intelligenceGrowth,
        double agilityGrowth,
        double moveSpeed,
        double attackRange,
        int skillProfileId,
        double powerScale)
    {
        return new HeroGrowthProfile(
            heroId,
            primaryAttribute,
            baseStrength,
            baseIntelligence,
            baseAgility,
            strengthGrowth,
            intelligenceGrowth,
            agilityGrowth,
            BaseMaxHp: 220 * powerScale,
            BasePhysicalAttack: 12 * powerScale,
            BaseMagicPower: primaryAttribute == BattlePrimaryAttribute.Intelligence ? 18 * powerScale : 4 * powerScale,
            BasePhysicalArmor: 8 * powerScale,
            BaseMagicResist: 7 * powerScale,
            BasePhysicalCrit: primaryAttribute == BattlePrimaryAttribute.Agility ? 20 * powerScale : 8 * powerScale,
            BaseHpRegen: 1.0 * powerScale,
            BaseEnergyRegen: skillProfileId > 0 ? 0.22 : 0.10,
            BaseInterruptThreshold: 18 * powerScale,
            MoveSpeed: moveSpeed,
            AttackRange: attackRange,
            AttackInterval: attackRange <= 1.2 ? 1.0 : 1.25,
            UltimateRadius: skillProfileId > 0 ? 2.0 : 0,
            UltimateEnergyCost: 1.0,
            UltimateCooldown: skillProfileId > 0 ? 6.0 : 0,
            Radius: 0.25,
            AggroRange: 7.0);
    }

    private static HeroGrowthProfile CreateMonsterProfile(
        OwnedMonster monster,
        BattlePrimaryAttribute primaryAttribute,
        int formationIndex)
    {
        int skillProfileId = ResolveSkillProfileId(primaryAttribute, monster.Position, formationIndex);
        double powerScale = ResolveBaseProfileScale(monster);
        MonsterTemplateConfig template = _configs?.GetMonsterTemplate(monster.TemplateId)
            ?? throw new InvalidOperationException($"Monster template '{monster.TemplateId}' was not found.");

        return CreateProfile(
            $"monster-{monster.Id}",
            primaryAttribute,
            skillProfileId,
            template,
            powerScale);
    }

    private static HeroGrowthProfile CreateProfile(
        string heroId,
        BattlePrimaryAttribute primaryAttribute,
        int skillProfileId,
        MonsterTemplateConfig template,
        double templateScale)
    {
        return new HeroGrowthProfile(
            heroId,
            primaryAttribute,
            template.BaseStrength,
            template.BaseIntelligence,
            template.BaseAgility,
            template.StrengthGrowth,
            template.IntelligenceGrowth,
            template.AgilityGrowth,
            BaseMaxHp: template.BaseMaxHp * templateScale,
            BasePhysicalAttack: template.BasePhysicalAttack * templateScale,
            BaseMagicPower: template.BaseMagicPower * templateScale,
            BasePhysicalArmor: template.BasePhysicalArmor * templateScale,
            BaseMagicResist: template.BaseMagicResist * templateScale,
            BasePhysicalCrit: template.BasePhysicalCrit * templateScale,
            BaseHpRegen: template.BaseHpRegen * templateScale,
            BaseEnergyRegen: template.BaseEnergyRegen,
            BaseInterruptThreshold: template.BaseInterruptThreshold * templateScale,
            MoveSpeed: template.MoveSpeed,
            AttackRange: template.AttackRange,
            AttackInterval: template.AttackInterval,
            UltimateRadius: skillProfileId > 0 ? 2.0 : 0,
            UltimateEnergyCost: 1.0,
            UltimateCooldown: skillProfileId > 0 ? 6.0 : 0,
            Radius: 0.25,
            AggroRange: 7.0);
    }

    private static int ResolveSkillProfileId(int profileIndex)
        => BattleSkillProfiles.PlainDamageTest;

    private static int ResolveSkillProfileId(BattlePrimaryAttribute primaryAttribute, string position, int formationIndex)
        => BattleSkillProfiles.PlainDamageTest;

    private static BattlePrimaryAttribute ResolvePrimaryAttribute(string attribute)
    {
        if (attribute == "智" || attribute.Contains('智') || attribute.Contains('鏅'))
        {
            return BattlePrimaryAttribute.Intelligence;
        }

        if (attribute == "敏" || attribute.Contains('敏') || attribute.Contains('鏁'))
        {
            return BattlePrimaryAttribute.Agility;
        }

        return BattlePrimaryAttribute.Strength;
    }

    private static HeroRank ResolveRank(string rank)
        => rank switch
        {
            "白" => HeroRank.White,
            "绿" => HeroRank.Green,
            "蓝" => HeroRank.Blue,
            "紫" => HeroRank.Purple,
            "红" => HeroRank.Red,
            _ when rank.Contains('绿') || rank.Contains('缁') => HeroRank.Green,
            _ when rank.Contains('蓝') || rank.Contains('钃') => HeroRank.Blue,
            _ when rank.Contains('紫') || rank.Contains('绱') => HeroRank.Purple,
            _ when rank.Contains('红') || rank.Contains('绾') => HeroRank.Red,
            _ => HeroRank.White,
        };

    private static string ResolveStageRank(int stage)
        => Math.Clamp(stage, 1, GameAccountStore.MaxMonsterStage) switch
        {
            1 => "白",
            2 => "绿",
            3 => "蓝",
            4 => "紫",
            _ => "红",
        };

    private static double ResolveBaseProfileScale(OwnedMonster monster)
    {
        double starScale = 1.0 + (Math.Clamp(monster.Star, 1, 5) - 1) * 0.05;
        double stageScale = 1.0 + (Math.Clamp(monster.Stage, 1, GameAccountStore.MaxMonsterStage) - 1) * 0.055;
        double rankScale = ResolveRank(monster.Rank) switch
        {
            HeroRank.White => 0.78,
            HeroRank.Green => 0.82,
            HeroRank.Blue => 1.02,
            HeroRank.Purple => 1.24,
            HeroRank.Red => 1.52,
            _ => 0.78,
        };

        return Math.Clamp(starScale * stageScale * rankScale, 0.75, 1.85);
    }

    private static double ResolveCurrentEquipmentScale(OwnedMonster monster)
    {
        if (monster.Equipment.Count == 0)
        {
            return 0.0;
        }

        double equippedRate = monster.Equipment.Count(slot => slot.Equipped) / (double)monster.Equipment.Count;
        return Math.Clamp(equippedRate, 0.0, 1.0);
    }

    private static HeroAttributeBundle CreateEquippedStats(BattlePrimaryAttribute primaryAttribute)
        => primaryAttribute switch
        {
            BattlePrimaryAttribute.Intelligence => new HeroAttributeBundle(
                Intelligence: 32,
                MaxHp: 420,
                MagicPower: 95,
                PhysicalArmor: 12,
                MagicResist: 34,
                EnergyRegen: 0.03),
            BattlePrimaryAttribute.Agility => new HeroAttributeBundle(
                Agility: 34,
                MaxHp: 360,
                PhysicalAttack: 72,
                PhysicalArmor: 18,
                PhysicalCrit: 36,
                EnergyRegen: 0.02),
            _ => new HeroAttributeBundle(
                Strength: 36,
                MaxHp: 620,
                PhysicalAttack: 58,
                PhysicalArmor: 35,
                MagicResist: 14,
                HpRegen: 3),
        };

    private static HeroAttributeBundle CreateRankProgressionStats(BattlePrimaryAttribute primaryAttribute, HeroRank rank)
        => HeroAttributeCalculator.CalculateRankProgressionStats(rank, primaryAttribute);

    private static HeroAttributeBundle CreateDemonEssenceStats(BattlePrimaryAttribute primaryAttribute, int demonEssence, int stage)
    {
        int essence = Math.Clamp(demonEssence, 0, GameAccountStore.MaxDemonEssence);
        int clampedStage = Math.Clamp(stage, 1, GameAccountStore.MaxMonsterStage);
        double stageBonus = clampedStage - 1;

        HeroAttributeBundle stats = primaryAttribute switch
        {
            BattlePrimaryAttribute.Intelligence => new HeroAttributeBundle(
                Strength: essence * 0.7,
                Intelligence: essence * 2.0,
                Agility: essence * 0.8,
                MaxHp: essence * 18 + stageBonus * 90,
                MagicPower: essence * 4.2 + stageBonus * 18,
                MagicResist: essence * 0.55 + stageBonus * 3,
                EnergyRegen: essence * 0.002),
            BattlePrimaryAttribute.Agility => new HeroAttributeBundle(
                Strength: essence * 0.8,
                Intelligence: essence * 0.7,
                Agility: essence * 2.0,
                MaxHp: essence * 20 + stageBonus * 95,
                PhysicalAttack: essence * 3.4 + stageBonus * 15,
                PhysicalCrit: essence * 1.1 + stageBonus * 5,
                EnergyRegen: essence * 0.002),
            _ => new HeroAttributeBundle(
                Strength: essence * 2.0,
                Intelligence: essence * 0.7,
                Agility: essence * 0.8,
                MaxHp: essence * 28 + stageBonus * 130,
                PhysicalAttack: essence * 2.8 + stageBonus * 14,
                PhysicalArmor: essence * 0.8 + stageBonus * 4,
                HpRegen: essence * 0.12),
        };

        return stats with
        {
            InterruptThreshold = essence * 0.8 + stageBonus * 6,
        };
    }
}
