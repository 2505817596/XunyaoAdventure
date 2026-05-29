using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleWaveController
{
    private static readonly Fix32 NextWaveDelay = Fix32.FromInt(3);

    private readonly BattleWorld _world;
    private int _currentWaveIndex = -1;
    private int _pendingWaveIndex = -1;
    private Fix32 _nextWaveDelayRemaining;
    private bool _waveActive;
    private bool _returningToFormation;

    public BattleWaveController(BattleWorld world)
    {
        _world = world;
    }

    public int CurrentWaveIndex => _currentWaveIndex;
    public bool IsWaveActive => _waveActive;
    public bool IsReturningToFormation => _returningToFormation;
    public bool IsBetweenWaves => _returningToFormation || _nextWaveDelayRemaining > Fix32.Zero;

    public void StartFirstWave()
        => StartWave(0);

    public void EvaluateBattleState()
    {
        bool hasAlivePlayers = _world.HasAliveUnits(BattleTeam.TeamA);
        if (!hasAlivePlayers)
        {
            _world.EndBattle(BattleTeam.TeamB);
            return;
        }

        bool hasAliveEnemies = _world.HasAliveUnits(BattleTeam.TeamB);
        if (hasAliveEnemies)
        {
            return;
        }

        _waveActive = false;
        _world.AddBattleEvent(BattleEvent.WaveEnded(_currentWaveIndex));
        ResetPlayerFormation();
        BeginNextWaveDelay(_currentWaveIndex + 1);
    }

    public void ProcessBetweenWaves(Fix32 deltaTime)
    {
        if (!_world.HasAliveUnits(BattleTeam.TeamA))
        {
            _world.EndBattle(BattleTeam.TeamB);
            return;
        }

        if (_returningToFormation)
        {
            _returningToFormation = false;
            _nextWaveDelayRemaining = NextWaveDelay;
            return;
        }

        TickPlayersBetweenWaves(deltaTime);
        _nextWaveDelayRemaining -= deltaTime;
        if (_nextWaveDelayRemaining > Fix32.Zero)
        {
            return;
        }

        _nextWaveDelayRemaining = Fix32.Zero;
        int nextWave = _pendingWaveIndex;
        _pendingWaveIndex = -1;
        StartWave(nextWave);
    }

    public void MarkBattleEnded()
    {
        _waveActive = false;
    }

    private void StartWave(int waveIndex)
    {
        if (waveIndex >= _world.Setup.EnemyWaves.Count)
        {
            _world.EndBattle(_world.HasAliveUnits(BattleTeam.TeamA) ? BattleTeam.TeamA : BattleTeam.TeamB);
            return;
        }

        _currentWaveIndex = waveIndex;
        _world.StartEnemyWave(waveIndex);
        _waveActive = true;
        _world.AddBattleEvent(BattleEvent.WaveStarted(_currentWaveIndex));
    }

    private void BeginNextWaveDelay(int waveIndex)
    {
        if (waveIndex >= _world.Setup.EnemyWaves.Count)
        {
            _world.EndBattle(_world.HasAliveUnits(BattleTeam.TeamA) ? BattleTeam.TeamA : BattleTeam.TeamB);
            return;
        }

        _pendingWaveIndex = waveIndex;
        _returningToFormation = true;
        _nextWaveDelayRemaining = Fix32.Zero;
    }

    private void ResetPlayerFormation()
    {
        IReadOnlyList<int> playerUnitIds = _world.PlayerUnitIds;
        for (int i = 0; i < playerUnitIds.Count; i++)
        {
            if (!_world.TryGetUnit(playerUnitIds[i], out BattleUnit unit) || !unit.IsAlive)
            {
                continue;
            }

            unit.CancelAction();
            if (!_world.TryGetPlayerHomePosition(unit.Id, out FixVec2 homePosition))
            {
                continue;
            }

            unit.SetPosition(homePosition);
            _world.AddBattleEvent(BattleEvent.UnitMoved(unit.Id, unit.Position, unit.Team));
        }
    }

    private void TickPlayersBetweenWaves(Fix32 deltaTime)
    {
        IReadOnlyList<int> playerUnitIds = _world.PlayerUnitIds;
        for (int i = 0; i < playerUnitIds.Count; i++)
        {
            if (!_world.TryGetUnit(playerUnitIds[i], out BattleUnit unit) || !unit.IsAlive)
            {
                continue;
            }

            unit.ReduceCooldown(deltaTime);
            _world.TickUnitBuffs(unit, deltaTime);
        }
    }
}
