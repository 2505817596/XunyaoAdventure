namespace XunyaoAdventure.Game;

public sealed class GameSession
{
    public string? UserName { get; private set; }
    public CampaignStageSelection? SelectedStage { get; private set; }
    public CampaignBattleResult? LastBattleResult { get; private set; }
    public List<int> BattleMonsterIds { get; } = new();

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(UserName);

    public void SignIn(string userName)
    {
        UserName = userName;
    }

    public void SignOut()
    {
        UserName = null;
        SelectedStage = null;
        LastBattleResult = null;
        BattleMonsterIds.Clear();
    }

    public void SelectStage(CampaignStageSelection stage)
    {
        SelectedStage = stage;
        BattleMonsterIds.Clear();
    }

    public void EnsureSelectedStage(CampaignStageSelection stage)
    {
        SelectedStage ??= stage;
    }

    public void ClearSelectedStage()
    {
        SelectedStage = null;
    }

    public void SetBattleResult(CampaignBattleResult result)
    {
        LastBattleResult = result;
    }

    public void ClearBattleResult()
    {
        LastBattleResult = null;
    }

    public void SetBattleMonsters(IEnumerable<int> monsterIds, int formationSize)
    {
        BattleMonsterIds.Clear();
        BattleMonsterIds.AddRange(monsterIds.Take(Math.Max(1, formationSize)));
    }

    public void SetBattleMonsters(IEnumerable<int> monsterIds)
    {
        BattleMonsterIds.Clear();
        BattleMonsterIds.AddRange(monsterIds);
    }
}
