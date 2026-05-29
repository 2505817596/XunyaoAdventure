namespace XunyaoAdventure.Game;

public sealed class PlayerAccount
{
    public PlayerAccount(
        string userName,
        string password,
        PlayerProfile? profile,
        List<OwnedMonster> monsters,
        List<BackpackEquipmentItem> backpack,
        List<BackpackConsumableItem> consumables,
        List<BackpackFragmentItem> fragments,
        int level,
        int copper)
    {
        UserName = userName;
        Password = password;
        Profile = profile;
        Monsters = monsters;
        Backpack = backpack;
        Consumables = consumables;
        Fragments = fragments;
        Level = level;
        Copper = copper;
    }

    public string UserName { get; set; }
    public string Password { get; set; }
    public PlayerProfile? Profile { get; set; }
    public List<OwnedMonster> Monsters { get; set; }
    public List<BackpackEquipmentItem> Backpack { get; set; }
    public List<BackpackConsumableItem> Consumables { get; set; }
    public List<BackpackFragmentItem> Fragments { get; set; }
    public int Level { get; set; }
    public int Copper { get; set; }
}

public sealed class PlayerProfile
{
    public PlayerProfile(string name, DateTime createdAt)
    {
        Name = name;
        CreatedAt = createdAt;
    }

    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class OwnedMonster
{
    public OwnedMonster(
        int id,
        string templateId,
        string name,
        int level,
        int star,
        string rank,
        string attribute,
        string element,
        string position,
        int power,
        string portraitUrl,
        bool isFavorite,
        IReadOnlyList<MonsterEquipmentSlot> equipment,
        int experience,
        int stage,
        int demonEssence)
    {
        Id = id;
        TemplateId = templateId;
        Name = name;
        Level = level;
        Star = star;
        Rank = rank;
        Attribute = attribute;
        Element = element;
        Position = position;
        Power = power;
        PortraitUrl = portraitUrl;
        IsFavorite = isFavorite;
        Equipment = equipment.ToList();
        Experience = experience;
        Stage = stage;
        DemonEssence = demonEssence;
    }

    public int Id { get; set; }
    public string TemplateId { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int Star { get; set; }
    public string Rank { get; set; }
    public string Attribute { get; set; }
    public string Element { get; set; }
    public string Position { get; set; }
    public int Power { get; set; }
    public string PortraitUrl { get; set; }
    public bool IsFavorite { get; set; }
    public List<MonsterEquipmentSlot> Equipment { get; set; }
    public int Experience { get; set; }
    public int Stage { get; set; }
    public int DemonEssence { get; set; }
}

public sealed class BackpackEquipmentItem
{
    public BackpackEquipmentItem(
        int instanceId,
        string templateId,
        string name,
        string slotKey,
        string iconText,
        int power,
        string quality,
        string attributeText,
        HeroAttributeConfig stats,
        int? equippedMonsterId,
        int upgradeLevel)
    {
        InstanceId = instanceId;
        TemplateId = templateId;
        Name = name;
        SlotKey = slotKey;
        IconText = iconText;
        Power = power;
        Quality = quality;
        AttributeText = attributeText;
        Stats = stats;
        EquippedMonsterId = equippedMonsterId;
        UpgradeLevel = upgradeLevel;
    }

    public int InstanceId { get; set; }
    public string TemplateId { get; set; }
    public string Name { get; set; }
    public string SlotKey { get; set; }
    public string IconText { get; set; }
    public int Power { get; set; }
    public string Quality { get; set; }
    public string AttributeText { get; set; }
    public HeroAttributeConfig Stats { get; set; }
    public int? EquippedMonsterId { get; set; }
    public int UpgradeLevel { get; set; }
}

public sealed class MonsterEquipmentSlot
{
    public MonsterEquipmentSlot(string name, bool equipped, string iconText, string slotKey, int? itemInstanceId)
    {
        Name = name;
        Equipped = equipped;
        IconText = iconText;
        SlotKey = slotKey;
        ItemInstanceId = itemInstanceId;
    }

    public string Name { get; set; }
    public bool Equipped { get; set; }
    public string IconText { get; set; }
    public string SlotKey { get; set; }
    public int? ItemInstanceId { get; set; }
}

public sealed class BackpackConsumableItem
{
    public BackpackConsumableItem(string templateId, string name, string iconText, int quantity, string effectType, int effectValue, string description)
    {
        TemplateId = templateId;
        Name = name;
        IconText = iconText;
        Quantity = quantity;
        EffectType = effectType;
        EffectValue = effectValue;
        Description = description;
    }

    public string TemplateId { get; set; }
    public string Name { get; set; }
    public string IconText { get; set; }
    public int Quantity { get; set; }
    public string EffectType { get; set; }
    public int EffectValue { get; set; }
    public string Description { get; set; }
}

public sealed class BackpackFragmentItem
{
    public BackpackFragmentItem(string templateId, string name, string iconText, int quantity, string quality, string description)
    {
        TemplateId = templateId;
        Name = name;
        IconText = iconText;
        Quantity = quantity;
        Quality = quality;
        Description = description;
    }

    public string TemplateId { get; set; }
    public string Name { get; set; }
    public string IconText { get; set; }
    public int Quantity { get; set; }
    public string Quality { get; set; }
    public string Description { get; set; }
}

public sealed record GameLoginResult(bool Success, string Message, PlayerAccount? Account);

public sealed record GameEquipResult(bool Success, string Message, PlayerAccount? Account);

public sealed record GameLevelUpResult(
    bool Success,
    string Message,
    PlayerAccount? Account,
    int NewLevel,
    int NewExperience,
    int ConsumedPotionCount);

public sealed record GameMaterialUseResult(
    bool Success,
    string Message,
    PlayerAccount? Account,
    int ConsumedCount);

public enum RankingBoardType
{
    TotalPower,
    StrongestMonster,
    Level,
    Copper,
}

public sealed record RankingEntry(
    int Rank,
    string UserName,
    string RoleName,
    int Level,
    int Copper,
    int TotalPower,
    int StrongestMonsterPower,
    string StrongestMonsterName,
    DateTime CreatedAt);

public enum GuildMemberRole
{
    Leader,
    Member,
}

public sealed class Guild
{
    public Guild(int id, string name, string notice, string leaderUserName, List<GuildMember> members, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Notice = notice;
        LeaderUserName = leaderUserName;
        Members = members;
        CreatedAt = createdAt;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public string Notice { get; set; }
    public string LeaderUserName { get; set; }
    public List<GuildMember> Members { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class GuildMember
{
    public GuildMember(string userName, string roleName, GuildMemberRole role, DateTime joinedAt)
    {
        UserName = userName;
        RoleName = roleName;
        Role = role;
        JoinedAt = joinedAt;
    }

    public string UserName { get; set; }
    public string RoleName { get; set; }
    public GuildMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

public sealed record GuildActionResult(bool Success, string Message, Guild? Guild);
