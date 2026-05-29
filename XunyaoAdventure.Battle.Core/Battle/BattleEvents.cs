namespace LeanClr.Battle;

public readonly struct BattleEvent
{
    public readonly BattleEventType Type;
    public readonly int UnitId;
    public readonly int TargetUnitId;
    public readonly LeanClr.Mathematics.FixVec2 Position;
    public readonly LeanClr.Mathematics.Fix32 Amount;
    public readonly LeanClr.Mathematics.Fix32 RemainingHp;
    public readonly int WaveIndex;
    public readonly BattleTeam Team;
    public readonly BattleBuffType BuffType;

    public BattleEvent(
        BattleEventType type,
        int unitId,
        int targetUnitId,
        LeanClr.Mathematics.FixVec2 position,
        LeanClr.Mathematics.Fix32 amount,
        LeanClr.Mathematics.Fix32 remainingHp = default,
        int waveIndex = 0,
        BattleTeam team = BattleTeam.Neutral,
        BattleBuffType buffType = BattleBuffType.None)
    {
        Type = type;
        UnitId = unitId;
        TargetUnitId = targetUnitId;
        Position = position;
        Amount = amount;
        RemainingHp = remainingHp;
        WaveIndex = waveIndex;
        Team = team;
        BuffType = buffType;
    }

    public static BattleEvent BattleStarted()
        => new(BattleEventType.BattleStarted, 0, 0, LeanClr.Mathematics.FixVec2.Zero, LeanClr.Mathematics.Fix32.Zero);

    public static BattleEvent WaveStarted(int waveIndex)
        => new(BattleEventType.WaveStarted, 0, 0, LeanClr.Mathematics.FixVec2.Zero, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, waveIndex);

    public static BattleEvent WaveEnded(int waveIndex)
        => new(BattleEventType.WaveEnded, 0, 0, LeanClr.Mathematics.FixVec2.Zero, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, waveIndex);

    public static BattleEvent UnitSpawned(int unitId, LeanClr.Mathematics.FixVec2 position, LeanClr.Mathematics.Fix32 hp, BattleTeam team)
        => new(BattleEventType.UnitSpawned, unitId, 0, position, LeanClr.Mathematics.Fix32.Zero, hp, 0, team);

    public static BattleEvent UnitSpawned(int unitId, LeanClr.Mathematics.FixVec2 position, LeanClr.Mathematics.Fix32 hp, BattleTeam team, int profileId)
        => new(BattleEventType.UnitSpawned, unitId, 0, position, LeanClr.Mathematics.Fix32.FromInt(profileId), hp, 0, team, BattleBuffType.SummonLife);

    public static BattleEvent UnitSpawned(int unitId, LeanClr.Mathematics.FixVec2 position, LeanClr.Mathematics.Fix32 hp, BattleTeam team, int profileId, BattleBuffType buffType)
        => new(BattleEventType.UnitSpawned, unitId, 0, position, LeanClr.Mathematics.Fix32.FromInt(profileId), hp, 0, team, buffType);

    public static BattleEvent UnitSpawned(int unitId, LeanClr.Mathematics.FixVec2 position, BattleTeam team)
        => UnitSpawned(unitId, position, LeanClr.Mathematics.Fix32.Zero, team);

    public static BattleEvent UnitSpawned(int unitId, LeanClr.Mathematics.FixVec2 position)
        => new(BattleEventType.UnitSpawned, unitId, 0, position, LeanClr.Mathematics.Fix32.Zero);

    public static BattleEvent UnitMoved(int unitId, LeanClr.Mathematics.FixVec2 position, BattleTeam team)
        => new(BattleEventType.UnitMoved, unitId, 0, position, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent UnitAttack(int unitId, int targetUnitId, LeanClr.Mathematics.Fix32 damage, BattleTeam team)
        => new(BattleEventType.UnitAttack, unitId, targetUnitId, LeanClr.Mathematics.FixVec2.Zero, damage, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent UnitDamaged(int unitId, int sourceUnitId, LeanClr.Mathematics.Fix32 damage, LeanClr.Mathematics.Fix32 remainingHp, BattleTeam team)
        => new(BattleEventType.UnitDamaged, unitId, sourceUnitId, LeanClr.Mathematics.FixVec2.Zero, damage, remainingHp, 0, team);

    public static BattleEvent UnitDied(int unitId, int sourceUnitId, BattleTeam team)
        => new(BattleEventType.UnitDied, unitId, sourceUnitId, LeanClr.Mathematics.FixVec2.Zero, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent UltimateCast(int unitId, int targetUnitId, LeanClr.Mathematics.Fix32 damage, BattleTeam team)
        => new(BattleEventType.UltimateCast, unitId, targetUnitId, LeanClr.Mathematics.FixVec2.Zero, damage, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent UltimateHit(int unitId, int targetUnitId, LeanClr.Mathematics.Fix32 damage, BattleTeam team)
        => new(BattleEventType.UltimateHit, unitId, targetUnitId, LeanClr.Mathematics.FixVec2.Zero, damage, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent UltimateRecovered(int unitId, int targetUnitId, BattleTeam team)
        => new(BattleEventType.UltimateRecovered, unitId, targetUnitId, LeanClr.Mathematics.FixVec2.Zero, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent UltimateRejected(int unitId, int targetUnitId, BattleTeam team)
        => new(BattleEventType.UltimateRejected, unitId, targetUnitId, LeanClr.Mathematics.FixVec2.Zero, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent BattleEnded(BattleTeam winner)
        => new(BattleEventType.BattleEnded, 0, 0, LeanClr.Mathematics.FixVec2.Zero, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, 0, winner);

    public static BattleEvent UnitHealed(int unitId, int sourceUnitId, LeanClr.Mathematics.Fix32 amount, LeanClr.Mathematics.Fix32 remainingHp, BattleTeam team)
        => new(BattleEventType.UnitHealed, unitId, sourceUnitId, LeanClr.Mathematics.FixVec2.Zero, amount, remainingHp, 0, team);

    public static BattleEvent UnitEnergyChanged(int unitId, int sourceUnitId, LeanClr.Mathematics.Fix32 amount, LeanClr.Mathematics.Fix32 energy, BattleTeam team)
        => new(BattleEventType.UnitEnergyChanged, unitId, sourceUnitId, LeanClr.Mathematics.FixVec2.Zero, amount, energy, 0, team);

    public static BattleEvent BuffApplied(int unitId, int sourceUnitId, BattleBuff status, BattleTeam team)
        => new(BattleEventType.BuffApplied, unitId, sourceUnitId, LeanClr.Mathematics.FixVec2.Zero, status.Duration, LeanClr.Mathematics.Fix32.Zero, 0, team, status.Type);

    public static BattleEvent BuffExpired(int unitId, BattleBuffType buffType, BattleTeam team)
        => new(BattleEventType.BuffExpired, unitId, 0, LeanClr.Mathematics.FixVec2.Zero, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, 0, team, buffType);

    public static BattleEvent BuffTriggered(int unitId, int sourceUnitId, BattleBuffType buffType, LeanClr.Mathematics.Fix32 amount, BattleTeam team)
        => new(BattleEventType.BuffTriggered, unitId, sourceUnitId, LeanClr.Mathematics.FixVec2.Zero, amount, LeanClr.Mathematics.Fix32.Zero, 0, team, buffType);

    public static BattleEvent BuffDispelled(int unitId, int sourceUnitId, LeanClr.Mathematics.Fix32 amount, BattleTeam team)
        => new(BattleEventType.BuffDispelled, unitId, sourceUnitId, LeanClr.Mathematics.FixVec2.Zero, amount, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent ProjectileSpawned(int projectileId, int sourceUnitId, LeanClr.Mathematics.FixVec2 position, BattleTeam team)
        => new(BattleEventType.ProjectileSpawned, projectileId, sourceUnitId, position, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent ProjectileMoved(int projectileId, int targetUnitId, LeanClr.Mathematics.FixVec2 position, BattleTeam team)
        => new(BattleEventType.ProjectileMoved, projectileId, targetUnitId, position, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent ProjectileHit(int projectileId, int targetUnitId, LeanClr.Mathematics.FixVec2 position, BattleTeam team)
        => new(BattleEventType.ProjectileHit, projectileId, targetUnitId, position, LeanClr.Mathematics.Fix32.Zero, LeanClr.Mathematics.Fix32.Zero, 0, team);

    public static BattleEvent UnitRevived(int unitId, int sourceUnitId, LeanClr.Mathematics.Fix32 remainingHp, BattleTeam team)
        => new(BattleEventType.UnitRevived, unitId, sourceUnitId, LeanClr.Mathematics.FixVec2.Zero, LeanClr.Mathematics.Fix32.Zero, remainingHp, 0, team);
}

public readonly struct BattleCombatEvent
{
    public readonly BattleCombatEventType Type;
    public readonly int SourceUnitId;
    public readonly int TargetUnitId;
    public readonly LeanClr.Mathematics.Fix32 Amount;
    public readonly LeanClr.Mathematics.Fix32 Value;
    public readonly BattleTeam SourceTeam;
    public readonly BattleTeam TargetTeam;
    public readonly BattleBuffType BuffType;
    public readonly BattleSkillType SkillType;

    public BattleCombatEvent(
        BattleCombatEventType type,
        int sourceUnitId = 0,
        int targetUnitId = 0,
        LeanClr.Mathematics.Fix32 amount = default,
        LeanClr.Mathematics.Fix32 value = default,
        BattleTeam sourceTeam = BattleTeam.Neutral,
        BattleTeam targetTeam = BattleTeam.Neutral,
        BattleBuffType buffType = BattleBuffType.None,
        BattleSkillType skillType = BattleSkillType.None)
    {
        Type = type;
        SourceUnitId = sourceUnitId;
        TargetUnitId = targetUnitId;
        Amount = amount;
        Value = value;
        SourceTeam = sourceTeam;
        TargetTeam = targetTeam;
        BuffType = buffType;
        SkillType = skillType;
    }
}

