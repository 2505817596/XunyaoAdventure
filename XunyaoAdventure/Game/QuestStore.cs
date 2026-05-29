using LiteDB;
namespace XunyaoAdventure.Game;

public sealed class QuestStore
{
    private readonly object _gate = new();
    private readonly GameConfigStore _configs;
    private readonly GameDatabase _database;
    private readonly OperationLogStore _logs;
    private readonly Dictionary<string, List<QuestState>> _states = new(StringComparer.OrdinalIgnoreCase);

    public QuestStore(GameConfigStore configs, GameDatabase database, OperationLogStore logs)
    {
        _configs = configs;
        _database = database;
        _logs = logs;
        LoadStates();
    }

    public IReadOnlyList<QuestViewItem> GetQuestViews(string? userName, QuestCategory category)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Array.Empty<QuestViewItem>();
        }

        lock (_gate)
        {
            IReadOnlyList<QuestDefinition> definitions = GetDefinitions();
            List<QuestState> states = EnsureStates(userName, definitions);
            return definitions
                .Where(definition => definition.Category == category)
                .Select(definition => CreateViewItem(definition, states, definitions))
                .ToList();
        }
    }

    public void ReportEvent(string? userName, QuestEventType eventType, int count = 1)
    {
        if (string.IsNullOrWhiteSpace(userName) || count <= 0)
        {
            return;
        }

        lock (_gate)
        {
            string normalizedUserName = NormalizeUserName(userName);
            IReadOnlyList<QuestDefinition> definitions = GetDefinitions();
            List<QuestState> states = EnsureStates(normalizedUserName, definitions);
            bool changed = false;

            for (int i = 0; i < states.Count; i++)
            {
                QuestState state = states[i];
                QuestDefinition? definition = definitions.FirstOrDefault(item => item.Id == state.QuestId);
                if (definition is null
                    || definition.EventType != eventType
                    || state.Progress >= definition.RequiredCount
                    || !IsUnlocked(definition, states, definitions))
                {
                    continue;
                }

                state.Progress = Math.Min(definition.RequiredCount, state.Progress + count);
                changed = true;
            }

            if (changed)
            {
                SaveStates(normalizedUserName, states);
            }
        }
    }

    public QuestClaimResult ClaimReward(string? userName, string questId, GameAccountStore accounts)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new QuestClaimResult(false, "请先登录", null);
        }

        lock (_gate)
        {
            string normalizedUserName = NormalizeUserName(userName);
            IReadOnlyList<QuestDefinition> definitions = GetDefinitions();
            QuestDefinition? definition = definitions.FirstOrDefault(item => item.Id == questId);
            if (definition is null)
            {
                return new QuestClaimResult(false, "任务不存在", accounts.GetAccount(userName));
            }

            List<QuestState> states = EnsureStates(normalizedUserName, definitions);
            int stateIndex = states.FindIndex(item => item.QuestId == questId);
            if (stateIndex < 0)
            {
                return new QuestClaimResult(false, "任务状态不存在", accounts.GetAccount(userName));
            }

            QuestState state = states[stateIndex];
            if (!IsUnlocked(definition, states, definitions))
            {
                return new QuestClaimResult(false, "任务尚未解锁", accounts.GetAccount(userName));
            }

            if (state.Progress < definition.RequiredCount)
            {
                return new QuestClaimResult(false, "任务还未完成", accounts.GetAccount(userName));
            }

            if (state.RewardClaimed)
            {
                return new QuestClaimResult(false, "奖励已经领取", accounts.GetAccount(userName));
            }

            PlayerAccount? account = accounts.GrantRewards(userName, definition.Rewards);
            if (account is null)
            {
                return new QuestClaimResult(false, "账号不存在", null);
            }

            state.RewardClaimed = true;
            SaveStates(normalizedUserName, states);
            _logs.AppendRewardLog(
                normalizedUserName,
                OperationLogStore.QuestRewardClaimed,
                definition.Id,
                definition.Title,
                definition.Rewards);
            return new QuestClaimResult(true, $"领取成功：{FormatRewards(definition.Rewards)}", account);
        }
    }

    private IReadOnlyList<QuestDefinition> GetDefinitions()
        => _configs.GetQuestDefinitions()
            .Select(ToDefinition)
            .Where(definition => definition is not null)
            .Select(definition => definition!)
            .ToList();

    private static QuestDefinition? ToDefinition(QuestDefinitionConfig config)
    {
        if (!Enum.TryParse(config.Category, ignoreCase: true, out QuestCategory category)
            || !Enum.TryParse(config.EventType, ignoreCase: true, out QuestEventType eventType))
        {
            return null;
        }

        return new QuestDefinition(
            config.Id,
            category,
            config.Title,
            config.Description,
            eventType,
            config.RequiredCount,
            config.Rewards.Select(ToReward).Where(reward => reward is not null).Select(reward => reward!).ToList(),
            string.IsNullOrWhiteSpace(config.PreviousQuestId) ? null : config.PreviousQuestId);
    }

    private static QuestReward? ToReward(QuestRewardConfig config)
    {
        if (!Enum.TryParse(config.Type, ignoreCase: true, out QuestRewardType type))
        {
            return null;
        }

        return new QuestReward(
            type,
            config.Name,
            config.Quantity,
            string.IsNullOrWhiteSpace(config.TemplateId) ? null : config.TemplateId);
    }

    private static QuestViewItem CreateViewItem(
        QuestDefinition definition,
        List<QuestState> states,
        IReadOnlyList<QuestDefinition> definitions)
    {
        QuestState state = states.First(item => item.QuestId == definition.Id);
        bool unlocked = IsUnlocked(definition, states, definitions);
        bool completed = state.Progress >= definition.RequiredCount;
        return new QuestViewItem(definition, state.Progress, unlocked, completed, state.RewardClaimed);
    }

    private List<QuestState> EnsureStates(string userName, IReadOnlyList<QuestDefinition> definitions)
    {
        userName = NormalizeUserName(userName);
        bool changed = false;
        if (!_states.TryGetValue(userName, out List<QuestState>? states))
        {
            states = definitions
                .Select(definition => new QuestState(definition.Id, 0, false))
                .ToList();
            _states[userName] = states;
            SaveStates(userName, states);
            return states;
        }

        foreach (QuestDefinition definition in definitions)
        {
            if (!states.Any(state => state.QuestId == definition.Id))
            {
                states.Add(new QuestState(definition.Id, 0, false));
                changed = true;
            }
        }

        int removedCount = states.RemoveAll(state => !definitions.Any(definition => definition.Id == state.QuestId));
        changed = changed || removedCount > 0;
        if (changed)
        {
            SaveStates(userName, states);
        }

        return states;
    }

    private void LoadStates()
    {
        ILiteCollection<QuestStateDocument> collection = _database.GetCollection<QuestStateDocument>("quest_states");
        collection.EnsureIndex(state => state.UserName, unique: true);

        foreach (QuestStateDocument document in collection.FindAll())
        {
            if (string.IsNullOrWhiteSpace(document.UserName))
            {
                continue;
            }

            _states[NormalizeUserName(document.UserName)] = document.States
                .Select(state => new QuestState(state.QuestId, state.Progress, state.RewardClaimed))
                .ToList();
        }
    }

    private void SaveStates(string userName, IReadOnlyList<QuestState> states)
    {
        userName = NormalizeUserName(userName);
        ApplyStatesCache(userName, states);
        SaveStatesDocument(userName, states);
    }

    private void ApplyStatesCache(string userName, IReadOnlyList<QuestState> states)
    {
        _states[NormalizeUserName(userName)] = states.ToList();
    }

    private void SaveStatesDocument(string userName, IReadOnlyList<QuestState> states)
    {
        userName = NormalizeUserName(userName);
        ILiteCollection<QuestStateDocument> collection = _database.GetCollection<QuestStateDocument>("quest_states");
        collection.Upsert(new QuestStateDocument
        {
            UserName = userName,
            States = states
                .Select(state => new QuestStateItemDocument
                {
                    QuestId = state.QuestId,
                    Progress = state.Progress,
                    RewardClaimed = state.RewardClaimed,
                })
                .ToList(),
        });
    }

    private static bool IsUnlocked(
        QuestDefinition definition,
        List<QuestState> states,
        IReadOnlyList<QuestDefinition> definitions)
    {
        if (definition.PreviousQuestId is null)
        {
            return true;
        }

        QuestDefinition? previousDefinition = definitions.FirstOrDefault(item => item.Id == definition.PreviousQuestId);
        QuestState? previousState = states.FirstOrDefault(item => item.QuestId == definition.PreviousQuestId);
        return previousDefinition is not null
            && previousState is not null
            && previousState.RewardClaimed;
    }

    private static string FormatRewards(IReadOnlyList<QuestReward> rewards)
        => string.Join("，", rewards.Select(reward => $"{reward.Name} x{reward.Quantity}"));

    private static string NormalizeUserName(string userName)
        => userName.Trim();
}
