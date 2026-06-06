using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class CampaignStore
{
    private readonly object _gate = new();
    private readonly GameConfigStore _configs;
    private readonly GameDatabase _database;
    private readonly OperationLogStore _logs;
    private readonly Dictionary<string, HashSet<string>> _clearedStages = new(StringComparer.OrdinalIgnoreCase);

    public CampaignStore(GameConfigStore configs, GameDatabase database, OperationLogStore logs)
    {
        _configs = configs;
        _database = database;
        _logs = logs;
        LoadProgress();
    }

    public IReadOnlyList<CampaignChapterDefinition> GetChapters()
        => _configs.GetCampaignChapters()
            .Select(ToChapter)
            .OrderBy(chapter => chapter.Order)
            .ToList();

    public CampaignChapterDefinition? GetChapter(string chapterId)
        => GetChapters().FirstOrDefault(chapter => chapter.Id == chapterId);

    public IReadOnlyList<CampaignStage> GetStages(string chapterId)
        => GetAllStages()
            .Where(stage => stage.ChapterId == chapterId)
            .OrderBy(stage => stage.Order)
            .ToList();

    public IReadOnlyList<CampaignStageView> GetStageViews(string? userName, string chapterId)
    {
        List<CampaignStage> stages = GetAllStages()
            .Where(stage => stage.ChapterId == chapterId)
            .OrderBy(stage => stage.Order)
            .ToList();

        lock (_gate)
        {
            HashSet<string> cleared = GetClearedStages(userName);
            return stages
                .Select(stage => new CampaignStageView(
                    stage,
                    IsStageUnlocked(stage, cleared),
                    cleared.Contains(stage.Id)))
                .ToList();
        }
    }

    public CampaignStage? GetStage(string stageId)
        => GetAllStages().FirstOrDefault(stage => stage.Id == stageId);

    public CampaignStageSelection? CreateSelection(string? userName, string stageId)
    {
        CampaignStage? stage = GetStage(stageId);
        if (stage is null)
        {
            return null;
        }

        lock (_gate)
        {
            if (!IsStageUnlocked(stage, GetClearedStages(userName)))
            {
                return null;
            }
        }

        return new CampaignStageSelection(
            stage.Id,
            stage.Code,
            stage.Name,
            stage.Difficulty,
            stage.RecommendedPower);
    }

    public CampaignBattleResult CompleteBattle(string? userName, CampaignStageSelection? selection, bool victory, GameAccountStore accounts)
    {
        if (selection is null)
        {
            return new CampaignBattleResult(victory, "演练", "普通战斗", Array.Empty<QuestReward>(), false, null);
        }

        CampaignStage? stage = GetStage(selection.StageId);
        if (stage is null)
        {
            return new CampaignBattleResult(victory, selection.StageCode, selection.StageName, Array.Empty<QuestReward>(), false, null);
        }

        if (!victory || string.IsNullOrWhiteSpace(userName))
        {
            return new CampaignBattleResult(false, stage.Code, stage.Name, Array.Empty<QuestReward>(), false, null);
        }

        bool firstClear;
        List<QuestReward> rewards;
        lock (_gate)
        {
            string normalizedUserName = NormalizeUserName(userName);
            if (!_clearedStages.TryGetValue(normalizedUserName, out HashSet<string>? existingCleared))
            {
                existingCleared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            HashSet<string> updatedCleared = new(existingCleared, StringComparer.OrdinalIgnoreCase);
            firstClear = updatedCleared.Add(stage.Id);
            rewards = CreateStageRewards(stage, firstClear);
            PlayerAccount? rewardedAccount = accounts.GrantRewards(normalizedUserName, rewards);
            if (rewardedAccount is null)
            {
                return new CampaignBattleResult(false, stage.Code, stage.Name, Array.Empty<QuestReward>(), false, null);
            }

            if (firstClear)
            {
                _clearedStages[normalizedUserName] = updatedCleared;
                SaveProgressDocument(normalizedUserName, updatedCleared);
            }
            _logs.AppendRewardLog(
                normalizedUserName,
                firstClear ? OperationLogStore.StageCleared : OperationLogStore.StageSwept,
                stage.Id,
                $"{stage.Code} {stage.Name}",
                rewards);
        }

        CampaignStage? nextStage = GetNextStage(stage);

        return new CampaignBattleResult(
            true,
            stage.Code,
            stage.Name,
            rewards,
            firstClear,
            nextStage?.Code);
    }

    public CampaignBattleResult? SweepStage(string? userName, string stageId, GameAccountStore accounts)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        CampaignStage? stage = GetStage(stageId);
        if (stage is null)
        {
            return null;
        }

        lock (_gate)
        {
            if (!GetClearedStages(userName).Contains(stage.Id))
            {
                return null;
            }
        }

        List<QuestReward> rewards = CreateStageRewards(stage, firstClear: false);
        PlayerAccount? rewardedAccount = accounts.GrantRewards(userName, rewards);
        if (rewardedAccount is null)
        {
            return null;
        }

        _logs.AppendRewardLog(
            userName,
            OperationLogStore.StageSwept,
            stage.Id,
            $"{stage.Code} {stage.Name}",
            rewards);
        CampaignStage? nextStage = GetNextStage(stage);

        return new CampaignBattleResult(
            true,
            stage.Code,
            stage.Name,
            rewards,
            false,
            nextStage?.Code,
            Swept: true);
    }

    private static List<QuestReward> CreateStageRewards(CampaignStage stage, bool firstClear)
    {
        List<QuestReward> rewards = stage.Rewards.ToList();
        if (firstClear)
        {
            rewards.AddRange(stage.FirstClearRewards);
        }

        return rewards;
    }

    private CampaignStage? GetNextStage(CampaignStage stage)
        => GetAllStages()
            .Where(candidate => candidate.ChapterId == stage.ChapterId && candidate.Order > stage.Order)
            .OrderBy(candidate => candidate.Order)
            .FirstOrDefault();

    private HashSet<string> GetClearedStages(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName)
            || !_clearedStages.TryGetValue(userName, out HashSet<string>? cleared))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return cleared;
    }

    private void LoadProgress()
    {
        ILiteCollection<CampaignProgressDocument> collection = _database.GetCollection<CampaignProgressDocument>("campaign_progress");
        collection.EnsureIndex(progress => progress.UserName, unique: true);

        foreach (CampaignProgressDocument document in collection.FindAll())
        {
            if (string.IsNullOrWhiteSpace(document.UserName))
            {
                continue;
            }

            _clearedStages[NormalizeUserName(document.UserName)] = new HashSet<string>(
                document.ClearedStageIds.Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveProgress(string userName, HashSet<string> cleared)
    {
        userName = NormalizeUserName(userName);
        _clearedStages[userName] = cleared;
        SaveProgressDocument(userName, cleared);
    }

    private void SaveProgressDocument(string userName, HashSet<string> cleared)
    {
        ILiteCollection<CampaignProgressDocument> collection = _database.GetCollection<CampaignProgressDocument>("campaign_progress");
        collection.Upsert(new CampaignProgressDocument
        {
            UserName = userName,
            ClearedStageIds = cleared.OrderBy(id => id).ToList(),
        });
    }

    private bool IsStageUnlocked(CampaignStage stage, HashSet<string> cleared)
    {
        if (stage.BaseUnlocked)
        {
            return true;
        }

        CampaignStage? previousStage = GetAllStages()
            .Where(candidate => candidate.ChapterId == stage.ChapterId && candidate.Order < stage.Order)
            .OrderByDescending(candidate => candidate.Order)
            .FirstOrDefault();
        return previousStage is not null && cleared.Contains(previousStage.Id);
    }

    private List<CampaignStage> GetAllStages()
        => _configs.GetCampaignStages()
            .Select(ToStage)
            .ToList();

    private static CampaignChapterDefinition ToChapter(CampaignChapterConfig chapter)
        => new(chapter.Id, chapter.Name, chapter.Description, chapter.Order, chapter.Unlocked);

    private static CampaignStage ToStage(CampaignStageConfig stage)
        => new(
            stage.Id,
            stage.ChapterId,
            stage.Code,
            stage.Name,
            stage.Description,
            stage.Order,
            stage.RecommendedPower,
            stage.Difficulty,
            stage.BaseUnlocked,
            stage.Rewards.Select(ToQuestReward).ToList(),
            stage.FirstClearRewards.Select(ToQuestReward).ToList());

    private static QuestReward ToQuestReward(CampaignRewardConfig reward)
    {
        QuestRewardType type = Enum.TryParse(reward.Type, ignoreCase: true, out QuestRewardType parsed)
            ? parsed
            : QuestRewardType.Copper;
        return new QuestReward(
            type,
            reward.Name,
            reward.Quantity,
            string.IsNullOrWhiteSpace(reward.TemplateId) ? null : reward.TemplateId);
    }

    private static string NormalizeUserName(string userName)
        => userName.Trim();
}
