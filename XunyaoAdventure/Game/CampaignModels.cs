namespace XunyaoAdventure.Game;

public sealed record CampaignChapterDefinition(
    string Id,
    string Name,
    string Description,
    int Order,
    bool Unlocked);

public sealed record CampaignStage(
    string Id,
    string ChapterId,
    string Code,
    string Name,
    string Description,
    int Order,
    int RecommendedPower,
    int Difficulty,
    bool BaseUnlocked,
    IReadOnlyList<QuestReward> Rewards,
    IReadOnlyList<QuestReward> FirstClearRewards);

public sealed record CampaignStageView(
    CampaignStage Stage,
    bool Unlocked,
    bool Cleared);

public sealed record CampaignStageSelection(
    string StageId,
    string StageCode,
    string StageName,
    int Difficulty,
    int RecommendedPower);

public sealed record CampaignBattleResult(
    bool Victory,
    string StageCode,
    string StageName,
    IReadOnlyList<QuestReward> Rewards,
    bool FirstClear,
    string? NextStageCode,
    bool Swept = false);
