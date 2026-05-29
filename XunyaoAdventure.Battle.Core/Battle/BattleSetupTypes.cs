namespace LeanClr.Battle;

public readonly struct BattleUnitSpawn
{
    public readonly BattleUnitSpec Spec;
    public readonly LeanClr.Mathematics.FixVec2 Position;
    public readonly int SkillProfileId;

    public BattleUnitSpawn(
        LeanClr.Mathematics.Fix32 maxHp,
        LeanClr.Mathematics.Fix32 moveSpeed,
        LeanClr.Mathematics.Fix32 attackRange,
        LeanClr.Mathematics.Fix32 attackInterval,
        LeanClr.Mathematics.Fix32 physicalAttack,
        LeanClr.Mathematics.Fix32 ultimateDamage,
        LeanClr.Mathematics.Fix32 ultimateRadius,
        LeanClr.Mathematics.Fix32 ultimateEnergyCost,
        LeanClr.Mathematics.Fix32 energyRegenPerSecond,
        LeanClr.Mathematics.Fix32 ultimateCooldown,
        LeanClr.Mathematics.Fix32 radius,
        LeanClr.Mathematics.Fix32 aggroRange,
        BattleControlMode controlMode,
        LeanClr.Mathematics.FixVec2 position,
        int skillProfileId = 0,
        BattleUnitTag unitTags = BattleUnitTag.Hero,
        BattlePrimaryAttribute primaryAttribute = BattlePrimaryAttribute.Strength,
        int level = 1,
        LeanClr.Mathematics.Fix32 magicPower = default,
        LeanClr.Mathematics.Fix32 physicalArmor = default,
        LeanClr.Mathematics.Fix32 magicResist = default,
        LeanClr.Mathematics.Fix32 physicalCrit = default,
        LeanClr.Mathematics.Fix32 hpRegenPerSecond = default,
        LeanClr.Mathematics.Fix32 interruptThreshold = default)
    {
        Spec = new BattleUnitSpec(
            maxHp,
            moveSpeed,
            attackRange,
            attackInterval,
            physicalAttack,
            ultimateDamage,
            ultimateRadius,
            ultimateEnergyCost,
            energyRegenPerSecond,
            ultimateCooldown,
            radius,
            aggroRange,
            controlMode,
            unitTags,
            primaryAttribute,
            level,
            magicPower,
            physicalArmor,
            magicResist,
            physicalCrit,
            hpRegenPerSecond,
            interruptThreshold);
        Position = position;
        SkillProfileId = skillProfileId;
    }
}

public readonly struct BattleUnitSpec
{
    public readonly LeanClr.Mathematics.Fix32 MaxHp;
    public readonly LeanClr.Mathematics.Fix32 MoveSpeed;
    public readonly LeanClr.Mathematics.Fix32 AttackRange;
    public readonly LeanClr.Mathematics.Fix32 AttackInterval;
    public readonly LeanClr.Mathematics.Fix32 PhysicalAttack;
    public readonly LeanClr.Mathematics.Fix32 UltimateDamage;
    public readonly LeanClr.Mathematics.Fix32 UltimateRadius;
    public readonly LeanClr.Mathematics.Fix32 UltimateEnergyCost;
    public readonly LeanClr.Mathematics.Fix32 EnergyRegenPerSecond;
    public readonly LeanClr.Mathematics.Fix32 UltimateCooldown;
    public readonly LeanClr.Mathematics.Fix32 Radius;
    public readonly LeanClr.Mathematics.Fix32 AggroRange;
    public readonly BattleControlMode ControlMode;
    public readonly BattleUnitTag UnitTags;
    public readonly BattlePrimaryAttribute PrimaryAttribute;
    public readonly int Level;
    public readonly LeanClr.Mathematics.Fix32 MagicPower;
    public readonly LeanClr.Mathematics.Fix32 PhysicalArmor;
    public readonly LeanClr.Mathematics.Fix32 MagicResist;
    public readonly LeanClr.Mathematics.Fix32 PhysicalCrit;
    public readonly LeanClr.Mathematics.Fix32 HpRegenPerSecond;
    public readonly LeanClr.Mathematics.Fix32 InterruptThreshold;

    public BattleUnitSpec(
        LeanClr.Mathematics.Fix32 maxHp,
        LeanClr.Mathematics.Fix32 moveSpeed,
        LeanClr.Mathematics.Fix32 attackRange,
        LeanClr.Mathematics.Fix32 attackInterval,
        LeanClr.Mathematics.Fix32 physicalAttack,
        LeanClr.Mathematics.Fix32 ultimateDamage,
        LeanClr.Mathematics.Fix32 ultimateRadius,
        LeanClr.Mathematics.Fix32 ultimateEnergyCost,
        LeanClr.Mathematics.Fix32 energyRegenPerSecond,
        LeanClr.Mathematics.Fix32 ultimateCooldown,
        LeanClr.Mathematics.Fix32 radius,
        LeanClr.Mathematics.Fix32 aggroRange,
        BattleControlMode controlMode,
        BattleUnitTag unitTags = BattleUnitTag.Hero,
        BattlePrimaryAttribute primaryAttribute = BattlePrimaryAttribute.Strength,
        int level = 1,
        LeanClr.Mathematics.Fix32 magicPower = default,
        LeanClr.Mathematics.Fix32 physicalArmor = default,
        LeanClr.Mathematics.Fix32 magicResist = default,
        LeanClr.Mathematics.Fix32 physicalCrit = default,
        LeanClr.Mathematics.Fix32 hpRegenPerSecond = default,
        LeanClr.Mathematics.Fix32 interruptThreshold = default)
    {
        MaxHp = maxHp;
        MoveSpeed = moveSpeed;
        AttackRange = attackRange;
        AttackInterval = attackInterval;
        PhysicalAttack = physicalAttack;
        UltimateDamage = ultimateDamage;
        UltimateRadius = ultimateRadius;
        UltimateEnergyCost = ultimateEnergyCost;
        EnergyRegenPerSecond = energyRegenPerSecond;
        UltimateCooldown = ultimateCooldown;
        Radius = radius;
        AggroRange = aggroRange;
        ControlMode = controlMode;
        UnitTags = unitTags;
        PrimaryAttribute = primaryAttribute;
        Level = level > 0 ? level : 1;
        MagicPower = magicPower;
        PhysicalArmor = physicalArmor;
        MagicResist = magicResist;
        PhysicalCrit = physicalCrit;
        HpRegenPerSecond = hpRegenPerSecond;
        InterruptThreshold = interruptThreshold;
    }
}

public sealed class BattleWaveSetup
{
    public const int MaxUnitsPerTeam = 6;

    private readonly System.Collections.Generic.List<BattleUnitSpawn> _units = new();

    public int Count => _units.Count;
    public System.Collections.Generic.IReadOnlyList<BattleUnitSpawn> Units => _units;

    public bool TryAddUnit(BattleUnitSpawn spawn)
    {
        if (_units.Count >= MaxUnitsPerTeam)
        {
            return false;
        }

        _units.Add(spawn);
        return true;
    }
}

public sealed class BattleEncounterSetup
{
    public const int MaxPlayerUnits = 6;
    public const int MaxEnemyWaves = 3;

    private readonly System.Collections.Generic.List<BattleUnitSpawn> _playerUnits = new();
    private readonly System.Collections.Generic.List<BattleWaveSetup> _enemyWaves = new();

    public System.Collections.Generic.IReadOnlyList<BattleUnitSpawn> PlayerUnits => _playerUnits;
    public System.Collections.Generic.IReadOnlyList<BattleWaveSetup> EnemyWaves => _enemyWaves;

    public bool TryAddPlayerUnit(BattleUnitSpawn spawn)
    {
        if (_playerUnits.Count >= MaxPlayerUnits)
        {
            return false;
        }

        _playerUnits.Add(spawn);
        return true;
    }

    public BattleWaveSetup CreateEnemyWave()
    {
        if (_enemyWaves.Count >= MaxEnemyWaves)
        {
            throw new System.InvalidOperationException("Battle supports at most three enemy waves.");
        }

        BattleWaveSetup wave = new();
        _enemyWaves.Add(wave);
        return wave;
    }
}

