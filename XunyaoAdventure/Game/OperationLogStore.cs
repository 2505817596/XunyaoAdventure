using LiteDB;

namespace XunyaoAdventure.Game;

public sealed class OperationLogStore
{
    public const string QuestRewardClaimed = "QuestRewardClaimed";
    public const string StageCleared = "StageCleared";
    public const string StageSwept = "StageSwept";

    private readonly object _gate = new();
    private readonly GameDatabase _database;
    private long _nextLogId = 1;

    public OperationLogStore(GameDatabase database)
    {
        _database = database;
        LoadNextLogId();
    }

    public void AppendRewardLog(string? userName, string operationType, string subjectId, string summary, IReadOnlyList<QuestReward> rewards)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return;
        }

        OperationLogDocument document;
        lock (_gate)
        {
            document = new OperationLogDocument
            {
                Id = _nextLogId++,
                UserName = userName.Trim(),
                OperationType = operationType,
                CreatedAt = DateTimeOffset.UtcNow,
                SubjectId = subjectId,
                Summary = summary,
                CopperDelta = rewards
                    .Where(reward => reward.Type == QuestRewardType.Copper)
                    .Sum(reward => reward.Quantity),
                Rewards = rewards.Select(ToRewardDocument).ToList(),
            };
        }

        ILiteCollection<OperationLogDocument> collection = _database.GetCollection<OperationLogDocument>("operation_logs");
        collection.Insert(document);
    }

    private void LoadNextLogId()
    {
        ILiteCollection<OperationLogDocument> collection = _database.GetCollection<OperationLogDocument>("operation_logs");
        collection.EnsureIndex(log => log.Id, unique: true);
        collection.EnsureIndex(log => log.UserName);
        collection.EnsureIndex(log => log.OperationType);
        _nextLogId = collection.FindAll().Select(log => log.Id).DefaultIfEmpty(0).Max() + 1;
    }

    private static OperationRewardItemDocument ToRewardDocument(QuestReward reward)
        => new()
        {
            Type = reward.Type.ToString(),
            Name = reward.Name,
            TemplateId = reward.TemplateId ?? string.Empty,
            Quantity = reward.Quantity,
        };
}
