namespace XunyaoAdventure.Game;

public sealed class MonsterConfigFile
{
    public int Version { get; set; } = 1;
    public List<MonsterTemplateConfig> Monsters { get; set; } = new();
}

public sealed class MonsterTemplateConfig
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Attribute { get; set; } = "力";
    public string Element { get; set; } = "木";
    public string Position { get; set; } = "前排";
    public int InitialLevel { get; set; } = 1;
    public int InitialStar { get; set; } = 1;
    public int InitialStage { get; set; } = 1;
    public int InitialPower { get; set; } = 120;
    public string UnitAssetId { get; set; } = "001";
    public string PortraitUrl { get; set; } = "/battle-client/unit/001/stand/0000.png";
    public double BaseStrength { get; set; } = 26;
    public double BaseIntelligence { get; set; } = 14;
    public double BaseAgility { get; set; } = 18;
    public double StrengthGrowth { get; set; } = 2.9;
    public double IntelligenceGrowth { get; set; } = 1.4;
    public double AgilityGrowth { get; set; } = 1.9;
    public double BaseMaxHp { get; set; } = 220;
    public double BasePhysicalAttack { get; set; } = 12;
    public double BaseMagicPower { get; set; } = 4;
    public double BasePhysicalArmor { get; set; } = 8;
    public double BaseMagicResist { get; set; } = 7;
    public double BasePhysicalCrit { get; set; } = 8;
    public double BaseHpRegen { get; set; } = 1;
    public double BaseEnergyRegen { get; set; } = 0.22;
    public double BaseInterruptThreshold { get; set; } = 18;
    public double MoveSpeed { get; set; } = 2.15;
    public double AttackRange { get; set; } = 1.05;
    public double AttackInterval { get; set; } = 1.0;
}

public sealed class GameplayConfigFile
{
    public int Version { get; set; } = 1;
    public InitialRoleConfig InitialRole { get; set; } = new();
    public InitialAccountConfig InitialAccount { get; set; } = new();
}

public sealed class InitialRoleConfig
{
    public List<string> MonsterTemplateIds { get; set; } = new();
}

public sealed class InitialAccountConfig
{
    public int Level { get; set; } = 1;
    public int Copper { get; set; }
    public string DefaultStageId { get; set; } = string.Empty;
    public int FormationSize { get; set; } = 5;
}

public sealed class QuestConfigFile
{
    public int Version { get; set; } = 1;
    public List<QuestDefinitionConfig> Quests { get; set; } = new();
}

public sealed class QuestDefinitionConfig
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int RequiredCount { get; set; }
    public string PreviousQuestId { get; set; } = string.Empty;
    public List<QuestRewardConfig> Rewards { get; set; } = new();
}

public sealed class QuestRewardConfig
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string TemplateId { get; set; } = string.Empty;
}

public sealed class CampaignConfigFile
{
    public int Version { get; set; } = 1;
    public List<CampaignChapterConfig> Chapters { get; set; } = new();
    public List<CampaignStageConfig> Stages { get; set; } = new();
}

public sealed class CampaignChapterConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; } = 1;
    public bool Unlocked { get; set; }
}

public sealed class CampaignStageConfig
{
    public string Id { get; set; } = string.Empty;
    public string ChapterId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; } = 1;
    public int RecommendedPower { get; set; } = 180;
    public int Difficulty { get; set; } = 1;
    public bool BaseUnlocked { get; set; }
    public List<CampaignRewardConfig> Rewards { get; set; } = new();
    public List<CampaignRewardConfig> FirstClearRewards { get; set; } = new();
    public List<CampaignEnemyWaveConfig> Waves { get; set; } = new();
}

public sealed class CampaignRewardConfig
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string TemplateId { get; set; } = string.Empty;
}

public sealed class CampaignEnemyWaveConfig
{
    public List<CampaignEnemyConfig> Enemies { get; set; } = new();
}

public sealed class CampaignEnemyConfig
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 28;
    public int Star { get; set; } = 2;
    public int Stage { get; set; } = 3;
    public double X { get; set; } = 5.72;
    public double Y { get; set; } = -0.28;
}

public sealed class ItemConfigFile
{
    public int Version { get; set; } = 1;
    public List<ConsumableTemplateConfig> Consumables { get; set; } = new();
    public List<EquipmentTemplateConfig> Equipment { get; set; } = new();
    public List<FragmentTemplateConfig> Fragments { get; set; } = new();
}

public sealed class ConsumableTemplateConfig
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconText { get; set; } = "道";
    public string EffectType { get; set; } = "None";
    public int EffectValue { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class EquipmentTemplateConfig
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SlotKey { get; set; } = "weapon";
    public string IconText { get; set; } = "装";
    public string Quality { get; set; } = "白";
    public HeroAttributeConfig Stats { get; set; } = new();
    public int UpgradeLevel { get; set; }
}

public sealed class HeroAttributeConfig
{
    public double Strength { get; set; }
    public double Intelligence { get; set; }
    public double Agility { get; set; }
    public double MaxHp { get; set; }
    public double PhysicalAttack { get; set; }
    public double MagicPower { get; set; }
    public double PhysicalArmor { get; set; }
    public double MagicResist { get; set; }
    public double PhysicalCrit { get; set; }
    public double HpRegen { get; set; }
    public double EnergyRegen { get; set; }
    public double InterruptThreshold { get; set; }
}

public sealed class FragmentTemplateConfig
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconText { get; set; } = "碎";
    public string Quality { get; set; } = "白";
    public string Description { get; set; } = string.Empty;
}

public sealed class GachaConfigFile
{
    public int Version { get; set; } = 1;
    public List<GachaPoolConfig> Pools { get; set; } = new();
}

public sealed class GachaPoolConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CostItemTemplateId { get; set; } = string.Empty;
    public int PityDrawCount { get; set; }
    public int PityMaxWeight { get; set; }
    public List<GachaEntryConfig> Entries { get; set; } = new();
}

public sealed class GachaEntryConfig
{
    public string Type { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconText { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public int Weight { get; set; } = 1;
}

public sealed record ConfigSaveResult(bool Success, string Message);
