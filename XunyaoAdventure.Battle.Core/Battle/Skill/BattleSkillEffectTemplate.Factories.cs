using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly partial struct BattleSkillEffectTemplate
{
    public static BattleSkillEffectTemplate FormulaDamagePrimary(BattleDamageType damageType, BattleFormulaSpec amountFormula)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy, damageType, amountFormula);

    public static BattleSkillEffectTemplate DamagePrimary(BattleSkillValueSource amountSource)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate DamageEnemiesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.EnemyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate DamageEnemiesInLine(BattleSkillValueSource amountSource, BattleSkillValueSource widthSource)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.EnemyUnitsInLine, amountSource, Fix32.Zero, widthSource, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate DamageEnemiesInFan(BattleSkillValueSource amountSource, BattleSkillValueSource distanceSource)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.EnemyUnitsInFan, amountSource, Fix32.Zero, distanceSource, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate DamageChainedEnemies(BattleSkillValueSource amountSource, BattleSkillValueSource jumpRadiusSource, int maxTargets)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.ChainedEnemies, amountSource, Fix32.Zero, jumpRadiusSource, Fix32.Zero, maxTargets, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate DamageBackEnemy(BattleSkillValueSource amountSource)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.BackEnemy, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate DamageMiddleEnemyRow(BattleSkillValueSource amountSource)
        => new(BattleSkillEffectType.Damage, BattleSkillEffectTarget.MiddleEnemyRow, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate ExecuteEnemiesInRadius(Fix32 hpThreshold, BattleSkillValueSource radiusSource)
        => new(BattleSkillEffectType.Execute, BattleSkillEffectTarget.EnemyUnitsInRadius, BattleSkillValueSource.Constant, hpThreshold, radiusSource, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy, BattleDamageType.Pure);

    public static BattleSkillEffectTemplate HealAlliesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource)
        => new(BattleSkillEffectType.Heal, BattleSkillEffectTarget.AllyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate HealFrontAllyRow(BattleSkillValueSource amountSource)
        => new(BattleSkillEffectType.Heal, BattleSkillEffectTarget.FrontAllyRow, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate ModifyEnergyPrimary(BattleSkillValueSource amountSource)
        => new(BattleSkillEffectType.ModifyEnergy, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate StunPrimary(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Stun, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate StunEnemiesInLine(Fix32 duration, BattleSkillValueSource widthSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.EnemyUnitsInLine, BattleSkillValueSource.Constant, Fix32.Zero, widthSource, Fix32.Zero, 0, BattleBuffType.Stun, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate StunBackEnemy(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.BackEnemy, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Stun, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate PhysicalArmorUpPrimary(BattleSkillValueSource amountSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.PhysicalArmorUp, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate ShieldPrimary(BattleSkillValueSource amountSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Shield, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate ShieldSelf(BattleSkillValueSource amountSource, Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Shield, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectTemplate ShieldAlliesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.AllyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.Shield, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate ShieldFrontAllyRow(BattleSkillValueSource amountSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.FrontAllyRow, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Shield, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate ThornsPrimary(BattleSkillValueSource amountSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Thorns, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate LifeStealPrimary(BattleSkillValueSource amountSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.LifeSteal, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate CleansePrimary(int maxCount)
        => new(BattleSkillEffectType.Dispel, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillEffectDispelValue.Pack(maxCount, BattleDispelMode.DebuffOnly), BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate PurgePrimary(int maxCount)
        => new(BattleSkillEffectType.Dispel, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillEffectDispelValue.Pack(maxCount, BattleDispelMode.BuffOnly), BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate RagePrimary(BattleSkillValueSource amountSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Rage, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate SecondWindPrimary(BattleSkillValueSource amountSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.SecondWind, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate DeathBurstPrimary(BattleSkillValueSource amountSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.DeathBurst, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate SlowPrimary(Fix32 amount, Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, amount, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Slow, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate SlowEnemiesInFan(Fix32 amount, Fix32 duration, BattleSkillValueSource distanceSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.EnemyUnitsInFan, BattleSkillValueSource.Constant, amount, distanceSource, Fix32.Zero, 0, BattleBuffType.Slow, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate SilencePrimary(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Silence, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate SilenceEnemiesInRadius(Fix32 duration, BattleSkillValueSource radiusSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.EnemyUnitsInRadius, BattleSkillValueSource.Constant, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.Silence, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate SilenceChainedEnemies(Fix32 duration, BattleSkillValueSource jumpRadiusSource, int maxTargets)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.ChainedEnemies, BattleSkillValueSource.Constant, Fix32.Zero, jumpRadiusSource, Fix32.Zero, maxTargets, BattleBuffType.Silence, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate SilenceMiddleEnemyRow(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.MiddleEnemyRow, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Silence, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate DamageOverTimePrimary(BattleSkillValueSource amountSource, Fix32 tickInterval, Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, amountSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, tickInterval.Raw, BattleBuffType.DamageOverTime, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate DamageOverTimeEnemiesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource, Fix32 tickInterval, Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.EnemyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, tickInterval.Raw, BattleBuffType.DamageOverTime, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate HealOverTimeAlliesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource, Fix32 tickInterval, Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.AllyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, tickInterval.Raw, BattleBuffType.HealOverTime, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate TauntSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Taunt, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectTemplate DamageImmuneSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.DamageImmune, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectTemplate PhysicalImmuneSelf(Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.Self, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.PhysicalImmune, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectTemplate DamageTakenAmplifyPrimary(Fix32 rate, Fix32 duration)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, rate, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.DamageTakenAmplify, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate DisplacePrimary(Fix32 distance)
        => new(BattleSkillEffectType.Displace, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, distance, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate InterruptEnemiesInRadius(BattleSkillValueSource radiusSource)
        => new(BattleSkillEffectType.Interrupt, BattleSkillEffectTarget.EnemyUnitsInRadius, BattleSkillValueSource.Constant, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate RevivePrimary(BattleSkillValueSource reviveRatioSource)
        => new(BattleSkillEffectType.Revive, BattleSkillEffectTarget.PrimaryTarget, reviveRatioSource, Fix32.Zero, BattleSkillValueSource.Constant, Fix32.Zero, 0, BattleBuffType.Revive, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate SummonPrimary(Fix32 hpScale, int skillProfileId)
        => new(BattleSkillEffectType.Summon, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, hpScale, BattleSkillValueSource.Constant, Fix32.Zero, skillProfileId, BattleBuffType.None, BattleSkillValueSource.Constant, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate SummonPrimary(Fix32 hpScale, int skillProfileId, Fix32 duration)
        => new(BattleSkillEffectType.Summon, BattleSkillEffectTarget.PrimaryTarget, BattleSkillValueSource.Constant, hpScale, BattleSkillValueSource.Constant, Fix32.Zero, skillProfileId, BattleBuffType.None, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate SummonSelf(Fix32 hpScale, int skillProfileId, Fix32 duration)
        => new(BattleSkillEffectType.Summon, BattleSkillEffectTarget.Self, BattleSkillValueSource.Constant, hpScale, BattleSkillValueSource.Constant, Fix32.Zero, skillProfileId, BattleBuffType.None, BattleSkillValueSource.Constant, duration, BattleSkillTargetTeam.Self);

    public static BattleSkillEffectTemplate PhysicalAttackUpAlliesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.AllyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.PhysicalAttackUp, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate AttackSpeedUpAlliesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.AllyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.AttackSpeedUp, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate PhysicalArmorUpAlliesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.AllyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.PhysicalArmorUp, durationSource, Fix32.Zero, BattleSkillTargetTeam.Ally);

    public static BattleSkillEffectTemplate AttackSpeedDownEnemiesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.EnemyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.AttackSpeedDown, durationSource, Fix32.Zero, BattleSkillTargetTeam.Enemy);

    public static BattleSkillEffectTemplate MagicResistDownEnemiesInRadius(BattleSkillValueSource amountSource, BattleSkillValueSource radiusSource, BattleSkillValueSource durationSource)
        => new(BattleSkillEffectType.ApplyStatus, BattleSkillEffectTarget.EnemyUnitsInRadius, amountSource, Fix32.Zero, radiusSource, Fix32.Zero, 0, BattleBuffType.MagicResistDown, durationSource, Fix32.Zero, BattleSkillTargetTeam.Enemy);
}
