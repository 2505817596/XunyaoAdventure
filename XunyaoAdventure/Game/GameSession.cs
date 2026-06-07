namespace XunyaoAdventure.Game;

public sealed class GameSession
{
    public string? UserName { get; private set; }
    public CampaignStageSelection? SelectedStage { get; private set; }
    public CampaignBattleResult? LastBattleResult { get; private set; }
    public ArenaBattleSelection? SelectedArenaBattle { get; private set; }
    public ArenaBattleResult? LastArenaBattleResult { get; private set; }
    private readonly List<int> _battleMonsterIds = new();
    public IReadOnlyList<int> BattleMonsterIds => _battleMonsterIds;

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(UserName);
    public bool IsArenaBattle => SelectedArenaBattle is not null;

    public void SignIn(string userName)
    {
        UserName = userName;
    }

    public void SignOut()
    {
        UserName = null;
        SelectedStage = null;
        LastBattleResult = null;
        SelectedArenaBattle = null;
        LastArenaBattleResult = null;
        _battleMonsterIds.Clear();
    }

    public void SelectStage(CampaignStageSelection stage)
    {
        SelectedStage = stage;
        SelectedArenaBattle = null;
        LastArenaBattleResult = null;
        _battleMonsterIds.Clear();
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
        LastArenaBattleResult = null;
    }

    public void ClearBattleResult()
    {
        LastBattleResult = null;
        LastArenaBattleResult = null;
    }

    public void SelectArenaBattle(ArenaBattleSelection selection)
    {
        SelectedArenaBattle = selection;
        SelectedStage = null;
        LastBattleResult = null;
        LastArenaBattleResult = null;
        _battleMonsterIds.Clear();
    }

    public void SetArenaBattleResult(ArenaBattleResult result)
    {
        LastArenaBattleResult = result;
        LastBattleResult = null;
    }

    public void ClearArenaBattle()
    {
        SelectedArenaBattle = null;
    }

    public void SetBattleMonsters(IEnumerable<int> monsterIds, int formationSize)
    {
        _battleMonsterIds.Clear();
        _battleMonsterIds.AddRange(monsterIds.Take(Math.Max(1, formationSize)));
    }

    public void SetBattleMonsters(IEnumerable<int> monsterIds)
    {
        _battleMonsterIds.Clear();
        _battleMonsterIds.AddRange(monsterIds);
    }
}
