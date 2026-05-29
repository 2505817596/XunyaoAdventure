using System.Collections.Generic;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleWorld
{
    private readonly BattleEncounterSetup _setup;
    private readonly BattleSkillEffectExecutor _skillEffectExecutor;
    private readonly BattleSkillCastController _skillCastController;
    private readonly BattleDamageResolver _damageResolver;
    private readonly BattleEffectResolver _effectResolver;
    private readonly BattleUnitLifecycleController _unitLifecycleController;
    private readonly BattleWaveController _waveController;
    private readonly BattleTargetSelector _targetSelector;
    private readonly BattlePassiveTriggerResolver _passiveTriggerResolver;
    private readonly BattleStatusTickController _statusTickController;
    private readonly Dictionary<int, BattleUnit> _units = new();
    private readonly List<int> _unitOrder = new();
    private readonly List<int> _playerUnitIds = new();
    private readonly List<int> _enemyUnitIds = new();
    private readonly List<BattleEvent> _events = new();
    private readonly List<BattleBuffTrigger> _combatEventTriggers = new();
    private readonly List<BattleProjectile> _projectiles = new();
    private readonly List<int> _projectilesToRemove = new();
    private readonly List<BattleUnit> _projectileTargets = new();
    private readonly Dictionary<int, FixVec2> _playerHomePositions = new();
    private FixRandom _random = new(0x6D2B79F5u);
    private int _nextUnitId = 1;
    private int _nextProjectileId = 1;
    private bool _battleEnded;
    private bool _battleStarted;

    public BattleWorld(BattleEncounterSetup setup)
    {
        _setup = setup;
        _skillEffectExecutor = new BattleSkillEffectExecutor(this);
        _skillCastController = new BattleSkillCastController(this, _skillEffectExecutor);
        _damageResolver = new BattleDamageResolver(this);
        _effectResolver = new BattleEffectResolver(this);
        _unitLifecycleController = new BattleUnitLifecycleController(this);
        _waveController = new BattleWaveController(this);
        _targetSelector = new BattleTargetSelector(this);
        _passiveTriggerResolver = new BattlePassiveTriggerResolver(this, _skillEffectExecutor);
        _statusTickController = new BattleStatusTickController(this, _passiveTriggerResolver);
    }

    public int TickCount { get; private set; }
    public BattleTeam Winner { get; private set; } = BattleTeam.Neutral;
    public bool IsBattleEnded => _battleEnded;
    public bool IsBattleStarted => _battleStarted;
    public int CurrentWaveIndex => _waveController.CurrentWaveIndex;
    public bool IsWaveActive => _waveController.IsWaveActive;
    public bool IsReturningToFormation => _waveController.IsReturningToFormation;

    public IReadOnlyDictionary<int, BattleUnit> Units => _units;
    internal IReadOnlyList<BattleProjectile> Projectiles => _projectiles;
    internal IReadOnlyList<int> UnitOrder => _unitOrder;
    internal IReadOnlyList<int> PlayerUnitIds => _playerUnitIds;
    public IReadOnlyList<BattleEvent> Events => _events;
    public BattleEncounterSetup Setup => _setup;
    public uint RandomState => _random.Seed;
    internal int NextRandomInt(int maxExclusive) => _random.NextInt(maxExclusive);

    public void ClearEvents() => _events.Clear();

    public int GetAliveUnitCount(BattleTeam team)
    {
        int count = 0;
        for (int i = 0; i < _unitOrder.Count; i++)
        {
            BattleUnit unit = _units[_unitOrder[i]];
            if (unit.IsAlive && unit.Team == team)
            {
                count++;
            }
        }

        return count;
    }

}
