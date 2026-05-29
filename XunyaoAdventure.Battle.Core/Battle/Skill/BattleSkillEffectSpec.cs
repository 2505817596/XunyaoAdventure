using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly partial struct BattleSkillEffectSpec
{
    public readonly BattleSkillEffectType Type;
    public readonly BattleSkillEffectTarget Target;
    public readonly Fix32 Amount;
    public readonly Fix32 Radius;
    public readonly int Value;
    public readonly BattleBuffType BuffType;
    public readonly Fix32 StatusDuration;
    public readonly BattleSkillTargetTeam TargetTeam;
    public readonly BattleDamageType DamageType;
    public readonly BattleFormulaSpec AmountFormula;
    public readonly BattleFormulaSpec RadiusFormula;
    public readonly BattleFormulaSpec StatusDurationFormula;
    public readonly BattleSkillTag Tags;
    public readonly BattleSkillEffectCondition Condition;

    public BattleSkillEffectSpec(
        BattleSkillEffectType type,
        BattleSkillEffectTarget target,
        Fix32 amount,
        Fix32 radius,
        int value = 0,
        BattleBuffType buffType = BattleBuffType.None,
        Fix32 statusDuration = default,
        BattleSkillTargetTeam targetTeam = BattleSkillTargetTeam.Enemy,
        BattleDamageType damageType = BattleDamageType.Physical,
        BattleFormulaSpec amountFormula = default,
        BattleFormulaSpec radiusFormula = default,
        BattleFormulaSpec statusDurationFormula = default,
        BattleSkillTag tags = BattleSkillTag.None,
        BattleSkillEffectCondition condition = default)
    {
        Type = type;
        Target = target;
        Amount = amount;
        Radius = radius;
        Value = value;
        BuffType = buffType;
        StatusDuration = statusDuration;
        TargetTeam = targetTeam;
        DamageType = damageType;
        AmountFormula = amountFormula;
        RadiusFormula = radiusFormula;
        StatusDurationFormula = statusDurationFormula;
        Tags = tags | BattleSkillTagUtility.InferEffectTags(type, buffType);
        Condition = condition;
    }

    public Fix32 ResolveAmount(BattleUnit source, BattleUnit target)
        => AmountFormula.Evaluate(source, target, Amount);

    public Fix32 ResolveRadius(BattleUnit source, BattleUnit target)
        => RadiusFormula.Evaluate(source, target, Radius);

    public Fix32 ResolveStatusDuration(BattleUnit source, BattleUnit target)
        => StatusDurationFormula.Evaluate(source, target, StatusDuration);

    public int ResolveDispelCount()
        => BattleSkillEffectDispelValue.ResolveCount(Value);

    public BattleDispelMode ResolveDispelMode()
        => BattleSkillEffectDispelValue.ResolveMode(Value);

    internal static int PackDispelValue(int maxCount, BattleDispelMode mode)
        => BattleSkillEffectDispelValue.Pack(maxCount, mode);

    public static BattleSkillEffectSpec DamagePrimary(Fix32 amount)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.PrimaryTarget, amount, Fix32.Zero);

    public static BattleSkillEffectSpec DamageEnemiesInRadius(Fix32 amount, Fix32 radius)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.EnemyUnitsInRadius, amount, radius);

    public static BattleSkillEffectSpec DamageEnemiesInLine(Fix32 amount, Fix32 width)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.EnemyUnitsInLine, amount, width);

    public static BattleSkillEffectSpec DamageEnemiesInFan(Fix32 amount, Fix32 distance)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.EnemyUnitsInFan, amount, distance);

    public static BattleSkillEffectSpec DamageChainedEnemies(Fix32 amount, Fix32 jumpRadius, int maxTargets)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.ChainedEnemies, amount, jumpRadius, maxTargets);

    public static BattleSkillEffectSpec DamageBackEnemy(Fix32 amount)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.BackEnemy, amount, Fix32.Zero);

    public static BattleSkillEffectSpec ExecutePrimary(Fix32 hpThreshold)
        => new(BattleSkillEffectType.Execute, BattleSkillEffectTarget.PrimaryTarget, hpThreshold, Fix32.Zero, 0, BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Enemy, BattleDamageType.Pure);

    public static BattleSkillEffectSpec StunPrimary(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.Stun, duration);

    public static BattleSkillEffectSpec StunEnemiesInRadius(Fix32 duration, Fix32 radius)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.EnemyUnitsInRadius, Fix32.Zero, radius, 0, BattleBuffType.Stun, duration);

    public static BattleSkillEffectSpec HealPrimary(Fix32 amount)
        => new(BattleSkillEffectType.Heal, BattleSkillEffectTarget.PrimaryTarget, amount, Fix32.Zero, 0, BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec HealAlliesInRadius(Fix32 amount, Fix32 radius)
        => new(BattleSkillEffectType.Heal, BattleSkillEffectTarget.AllyUnitsInRadius, amount, radius, 0, BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec ModifyEnergyPrimary(Fix32 amount)
        => new(BattleSkillEffectType.ModifyEnergy, BattleSkillEffectTarget.PrimaryTarget, amount, Fix32.Zero, 0, BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec ModifyEnergyAlliesInRadius(Fix32 amount, Fix32 radius)
        => new(BattleSkillEffectType.ModifyEnergy, BattleSkillEffectTarget.AllyUnitsInRadius, amount, radius, 0, BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec ModifyEnergyEnemiesInRadius(Fix32 amount, Fix32 radius)
        => new(BattleSkillEffectType.ModifyEnergy, BattleSkillEffectTarget.EnemyUnitsInRadius, amount, radius, 0, BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectSpec DispelPrimary(int maxCount)
        => new(BattleSkillEffectType.Dispel, BattleSkillEffectTarget.PrimaryTarget, Fix32.Zero, Fix32.Zero, PackDispelValue(maxCount, BattleDispelMode.Any), BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec DispelAlliesInRadius(int maxCount, Fix32 radius)
        => new(BattleSkillEffectType.Dispel, BattleSkillEffectTarget.AllyUnitsInRadius, Fix32.Zero, radius, PackDispelValue(maxCount, BattleDispelMode.Any), BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec CleansePrimary(int maxCount)
        => new(BattleSkillEffectType.Dispel, BattleSkillEffectTarget.PrimaryTarget, Fix32.Zero, Fix32.Zero, PackDispelValue(maxCount, BattleDispelMode.DebuffOnly), BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec PurgePrimary(int maxCount)
        => new(BattleSkillEffectType.Dispel, BattleSkillEffectTarget.PrimaryTarget, Fix32.Zero, Fix32.Zero, PackDispelValue(maxCount, BattleDispelMode.BuffOnly), BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectSpec SilencePrimary(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.Silence, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectSpec SilenceEnemiesInRadius(Fix32 duration, Fix32 radius)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.EnemyUnitsInRadius, Fix32.Zero, radius, 0, BattleBuffType.Silence, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectSpec DamageOverTimePrimary(Fix32 amountPerTick, Fix32 tickInterval, Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountPerTick, Fix32.Zero, tickInterval.Raw, BattleBuffType.DamageOverTime, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectSpec HealOverTimePrimary(Fix32 amountPerTick, Fix32 tickInterval, Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountPerTick, Fix32.Zero, tickInterval.Raw, BattleBuffType.HealOverTime, duration, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec TauntPrimary(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.Taunt, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectSpec TauntEnemiesInRadius(Fix32 duration, Fix32 radius)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.EnemyUnitsInRadius, Fix32.Zero, radius, 0, BattleBuffType.Taunt, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectSpec TauntSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.Taunt, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectSpec ControlImmuneSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.ControlImmune, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectSpec SilenceImmuneSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.SilenceImmune, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectSpec DisplaceImmuneSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.DisplaceImmune, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectSpec UninterruptibleSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.Uninterruptible, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectSpec DamageImmuneSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.DamageImmune, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectSpec PhysicalImmuneSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.PhysicalImmune, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectSpec MagicalImmuneSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.MagicalImmune, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectSpec PureImmuneSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, Fix32.Zero, Fix32.Zero, 0, BattleBuffType.PureImmune, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectSpec DisplacePrimary(Fix32 distance)
        => new(BattleSkillEffectType.Displace, BattleSkillEffectTarget.PrimaryTarget, distance, Fix32.Zero);

    public static BattleSkillEffectSpec DisplaceEnemiesInRadius(Fix32 distance, Fix32 radius)
        => new(BattleSkillEffectType.Displace, BattleSkillEffectTarget.EnemyUnitsInRadius, distance, radius, 0, BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectSpec InterruptPrimary()
        => new(BattleSkillEffectType.Interrupt, BattleSkillEffectTarget.PrimaryTarget, Fix32.Zero, Fix32.Zero);

    public static BattleSkillEffectSpec InterruptEnemiesInRadius(Fix32 radius)
        => new(BattleSkillEffectType.Interrupt, BattleSkillEffectTarget.EnemyUnitsInRadius, Fix32.Zero, radius, 0, BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectSpec RevivePrimary(Fix32 reviveRatio)
        => new(BattleSkillEffectType.Revive, BattleSkillEffectTarget.PrimaryTarget, reviveRatio, Fix32.Zero, 0, BattleBuffType.Revive, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec SummonPrimary(int skillProfileId, Fix32 hpScale)
        => new(BattleSkillEffectType.Summon, BattleSkillEffectTarget.PrimaryTarget, hpScale, Fix32.Zero, skillProfileId, BattleBuffType.None, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectSpec SummonSelf(int skillProfileId, Fix32 hpScale, Fix32 duration)
        => new(BattleSkillEffectType.Summon, BattleSkillEffectTarget.Self, hpScale, Fix32.Zero, skillProfileId, BattleBuffType.None, duration, BattleSkillTargetTeam.Self);
}

internal static class BattleSkillEffectDispelValue
{
    private const int DispelModeShift = 16;
    private const int DispelCountMask = 0xffff;

    public static int Pack(int maxCount, BattleDispelMode mode)
        => ((int)mode << DispelModeShift) | (maxCount & DispelCountMask);

    public static int ResolveCount(int value)
        => value & DispelCountMask;

    public static BattleDispelMode ResolveMode(int value)
        => (BattleDispelMode)(value >> DispelModeShift);
}

