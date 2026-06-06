namespace XunyaoAdventure.Game;

public enum QuestCategory
{
    Main,
    Daily,
}

public enum QuestEventType
{
    CreateCharacter,
    ViewMonsters,
    MonsterLevelUp,
    MonsterStarUp,
    UseDemonEssence,
    MonsterBreakthrough,
    EquipItem,
    UpgradeEquipment,
    FuseEquipment,
    EnterBattle,
    ClearStage,
    SendChat,
}

public enum QuestRewardType
{
    Copper,
    Consumable,
    Equipment,
}

public sealed record QuestDefinition(
    string Id,
    QuestCategory Category,
    string Title,
    string Description,
    QuestEventType EventType,
    int RequiredCount,
    IReadOnlyList<QuestReward> Rewards,
    string? PreviousQuestId = null);

public sealed record QuestReward(
    QuestRewardType Type,
    string Name,
    int Quantity,
    string? TemplateId = null);

public sealed class QuestState
{
    public QuestState(string questId, int progress, bool rewardClaimed)
    {
        QuestId = questId;
        Progress = progress;
        RewardClaimed = rewardClaimed;
    }

    public string QuestId { get; set; }
    public int Progress { get; set; }
    public bool RewardClaimed { get; set; }
}

public sealed record QuestViewItem(
    QuestDefinition Definition,
    int Progress,
    bool Unlocked,
    bool Completed,
    bool RewardClaimed);

public sealed record QuestClaimResult(bool Success, string Message, PlayerAccount? Account);
