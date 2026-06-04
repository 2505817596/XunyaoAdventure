using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class PlayerAccountDocument
{
    [BsonId]
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public PlayerProfileDocument? Profile { get; set; }
    public List<OwnedMonsterDocument> Monsters { get; set; } = new();
    public List<BackpackEquipmentItemDocument> Backpack { get; set; } = new();
    public List<BackpackConsumableItemDocument> Consumables { get; set; } = new();
    public List<BackpackFragmentItemDocument> Fragments { get; set; } = new();
    public Dictionary<string, int> GachaPityCounters { get; set; } = new();
    public int Level { get; set; }
    public int Copper { get; set; }
}

public sealed class PlayerProfileDocument
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class OwnedMonsterDocument
{
    public int Id { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Star { get; set; }
    public string Rank { get; set; } = string.Empty;
    public string Attribute { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int Power { get; set; }
    public string PortraitUrl { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public List<MonsterEquipmentSlotDocument> Equipment { get; set; } = new();
    public int Experience { get; set; }
    public int Stage { get; set; }
    public int DemonEssence { get; set; }
}

public sealed class MonsterEquipmentSlotDocument
{
    public string Name { get; set; } = string.Empty;
    public bool Equipped { get; set; }
    public string IconText { get; set; } = string.Empty;
    public string SlotKey { get; set; } = string.Empty;
    public int? ItemInstanceId { get; set; }
}

public sealed class BackpackEquipmentItemDocument
{
    public int InstanceId { get; set; }
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SlotKey { get; set; } = string.Empty;
    public string IconText { get; set; } = string.Empty;
    public int Power { get; set; }
    public string Quality { get; set; } = string.Empty;
    public string AttributeText { get; set; } = string.Empty;
    public HeroAttributeConfig Stats { get; set; } = new();
    public int? EquippedMonsterId { get; set; }
    public int UpgradeLevel { get; set; }
}

public sealed class BackpackConsumableItemDocument
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconText { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string EffectType { get; set; } = string.Empty;
    public int EffectValue { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class BackpackFragmentItemDocument
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconText { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Quality { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class GuildDocument
{
    [BsonId]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Notice { get; set; } = string.Empty;
    public string LeaderUserName { get; set; } = string.Empty;
    public List<GuildMemberDocument> Members { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public sealed class GuildMemberDocument
{
    public string UserName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public GuildMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

public sealed class ChatMessageDocument
{
    [BsonId]
    public long Id { get; set; }
    public ChatChannel Channel { get; set; }
    public string SenderUserName { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public bool IsSystem { get; set; }
    public int? GuildId { get; set; }
}

public sealed class QuestStateDocument
{
    [BsonId]
    public string UserName { get; set; } = string.Empty;
    public List<QuestStateItemDocument> States { get; set; } = new();
}

public sealed class QuestStateItemDocument
{
    public string QuestId { get; set; } = string.Empty;
    public int Progress { get; set; }
    public bool RewardClaimed { get; set; }
}

public sealed class CampaignProgressDocument
{
    [BsonId]
    public string UserName { get; set; } = string.Empty;
    public List<string> ClearedStageIds { get; set; } = new();
}

public sealed class OperationLogDocument
{
    [BsonId]
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string SubjectId { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int CopperDelta { get; set; }
    public List<OperationRewardItemDocument> Rewards { get; set; } = new();
}

public sealed class OperationRewardItemDocument
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class MailMessageDocument
{
    [BsonId]
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public bool Read { get; set; }
    public bool Claimed { get; set; }
    public List<OperationRewardItemDocument> Attachments { get; set; } = new();
}
