using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static partial class BattleConfigValidator
{
    private static bool IsDefinedEffectTarget(BattleSkillEffectTarget value)
        => value == BattleSkillEffectTarget.PrimaryTarget
            || value == BattleSkillEffectTarget.Self
            || value == BattleSkillEffectTarget.EnemyUnitsInRadius
            || value == BattleSkillEffectTarget.AllyUnitsInRadius
            || value == BattleSkillEffectTarget.AllEnemyUnits
            || value == BattleSkillEffectTarget.AllAllyUnits
            || value == BattleSkillEffectTarget.LowestHpEnemy
            || value == BattleSkillEffectTarget.LowestHpAlly
            || value == BattleSkillEffectTarget.RandomEnemy
            || value == BattleSkillEffectTarget.RandomAlly
            || value == BattleSkillEffectTarget.NearestEnemy
            || value == BattleSkillEffectTarget.NearestAlly
            || value == BattleSkillEffectTarget.FrontEnemy
            || value == BattleSkillEffectTarget.FrontAlly
            || value == BattleSkillEffectTarget.EnemyUnitsInLine
            || value == BattleSkillEffectTarget.AllyUnitsInLine
            || value == BattleSkillEffectTarget.EnemyUnitsInFan
            || value == BattleSkillEffectTarget.AllyUnitsInFan
            || value == BattleSkillEffectTarget.ChainedEnemies
            || value == BattleSkillEffectTarget.ChainedAllies
            || value == BattleSkillEffectTarget.BackEnemy
            || value == BattleSkillEffectTarget.BackAlly
            || value == BattleSkillEffectTarget.MiddleEnemy
            || value == BattleSkillEffectTarget.MiddleAlly
            || value == BattleSkillEffectTarget.FrontEnemyRow
            || value == BattleSkillEffectTarget.FrontAllyRow
            || value == BattleSkillEffectTarget.BackEnemyRow
            || value == BattleSkillEffectTarget.BackAllyRow
            || value == BattleSkillEffectTarget.MiddleEnemyRow
            || value == BattleSkillEffectTarget.MiddleAllyRow;

    private static bool RequiresPositiveRadius(BattleSkillEffectTarget value)
        => value == BattleSkillEffectTarget.EnemyUnitsInLine
            || value == BattleSkillEffectTarget.AllyUnitsInLine
            || value == BattleSkillEffectTarget.EnemyUnitsInFan
            || value == BattleSkillEffectTarget.AllyUnitsInFan
            || value == BattleSkillEffectTarget.ChainedEnemies
            || value == BattleSkillEffectTarget.ChainedAllies;

    private static bool RequiresPositiveTargetCount(BattleSkillEffectTarget value)
        => value == BattleSkillEffectTarget.ChainedEnemies
            || value == BattleSkillEffectTarget.ChainedAllies;

    private static bool IsDefinedTargetTeam(BattleSkillTargetTeam value)
        => value == BattleSkillTargetTeam.Enemy
            || value == BattleSkillTargetTeam.Ally
            || value == BattleSkillTargetTeam.Self;

    private static bool IsDefinedValueSource(BattleSkillValueSource value)
        => value == BattleSkillValueSource.Constant
            || value == BattleSkillValueSource.PhysicalAttack
            || value == BattleSkillValueSource.UltimateDamage
            || value == BattleSkillValueSource.UltimateRadius
            || value == BattleSkillValueSource.SourceMaxHp
            || value == BattleSkillValueSource.SourceCurrentHp
            || value == BattleSkillValueSource.SourceMissingHp
            || value == BattleSkillValueSource.SourceHpRatio
            || value == BattleSkillValueSource.SourceEnergy
            || value == BattleSkillValueSource.TargetMaxHp
            || value == BattleSkillValueSource.TargetCurrentHp
            || value == BattleSkillValueSource.TargetMissingHp
            || value == BattleSkillValueSource.TargetHpRatio
            || value == BattleSkillValueSource.TargetEnergy
            || value == BattleSkillValueSource.SourcePhysicalArmor
            || value == BattleSkillValueSource.SourceMagicResist
            || value == BattleSkillValueSource.TargetPhysicalArmor
            || value == BattleSkillValueSource.TargetMagicResist
            || value == BattleSkillValueSource.SourceMagicPower
            || value == BattleSkillValueSource.SourcePhysicalCrit
            || value == BattleSkillValueSource.TargetPhysicalCrit;

    private static bool IsDefinedFormulaOp(BattleFormulaOp value)
        => value == BattleFormulaOp.Add
            || value == BattleFormulaOp.Multiply;

    private static bool IsDefinedSkillTags(BattleSkillTag value)
    {
        const BattleSkillTag all =
            BattleSkillTag.Damage
            | BattleSkillTag.Heal
            | BattleSkillTag.Buff
            | BattleSkillTag.Debuff
            | BattleSkillTag.Control
            | BattleSkillTag.Shield
            | BattleSkillTag.Dispel
            | BattleSkillTag.Energy
            | BattleSkillTag.Displace
            | BattleSkillTag.Revive
            | BattleSkillTag.Summon
            | BattleSkillTag.Periodic
            | BattleSkillTag.Immunity
            | BattleSkillTag.NoLifeSteal
            | BattleSkillTag.NoReflection;

        return (value & ~all) == BattleSkillTag.None;
    }

    private static bool IsDefinedEffectCondition(BattleSkillEffectCondition value)
    {
        return value.Type == BattleSkillEffectConditionType.None
            || value.Type == BattleSkillEffectConditionType.SourceHpBelow
            || value.Type == BattleSkillEffectConditionType.SourceHpAbove
            || value.Type == BattleSkillEffectConditionType.TargetHpBelow
            || value.Type == BattleSkillEffectConditionType.TargetHpAbove
            || value.Type == BattleSkillEffectConditionType.SourceHasBuff
            || value.Type == BattleSkillEffectConditionType.TargetHasBuff
            || value.Type == BattleSkillEffectConditionType.SourceMissingBuff
            || value.Type == BattleSkillEffectConditionType.TargetMissingBuff
            || value.Type == BattleSkillEffectConditionType.TargetIsControlled
            || value.Type == BattleSkillEffectConditionType.TargetIsNotControlled
            || value.Type == BattleSkillEffectConditionType.SourceHasUnitTag
            || value.Type == BattleSkillEffectConditionType.TargetHasUnitTag
            || value.Type == BattleSkillEffectConditionType.SourceMissingUnitTag
            || value.Type == BattleSkillEffectConditionType.TargetMissingUnitTag;
    }

    private static bool IsDefinedDamageType(BattleDamageType value)
        => value == BattleDamageType.Physical
            || value == BattleDamageType.Magical
            || value == BattleDamageType.Pure
            || value == BattleDamageType.Healing
            || value == BattleDamageType.Shield
            || value == BattleDamageType.All;

    private static bool IsDefinedBuffType(BattleBuffType value)
        => value == BattleBuffType.None
            || value == BattleBuffType.Stun
            || value == BattleBuffType.PhysicalArmorUp
            || value == BattleBuffType.Shield
            || value == BattleBuffType.Thorns
            || value == BattleBuffType.LifeSteal
            || value == BattleBuffType.Rage
            || value == BattleBuffType.SecondWind
            || value == BattleBuffType.DeathBurst
            || value == BattleBuffType.Slow
            || value == BattleBuffType.Silence
            || value == BattleBuffType.PhysicalAttackUp
            || value == BattleBuffType.AttackSpeedUp
            || value == BattleBuffType.EnergyRegenUp
            || value == BattleBuffType.MagicResistUp
            || value == BattleBuffType.PhysicalAttackDown
            || value == BattleBuffType.AttackSpeedDown
            || value == BattleBuffType.EnergyRegenDown
            || value == BattleBuffType.PhysicalArmorDown
            || value == BattleBuffType.MagicResistDown
            || value == BattleBuffType.Taunt
            || value == BattleBuffType.DamageOverTime
            || value == BattleBuffType.HealOverTime
            || value == BattleBuffType.ControlImmune
            || value == BattleBuffType.SilenceImmune
            || value == BattleBuffType.DisplaceImmune
            || value == BattleBuffType.Uninterruptible
            || value == BattleBuffType.DamageImmune
            || value == BattleBuffType.PhysicalImmune
            || value == BattleBuffType.MagicalImmune
            || value == BattleBuffType.PureImmune
            || value == BattleBuffType.HealingAmplify
            || value == BattleBuffType.HealingReduce
            || value == BattleBuffType.DamageTakenAmplify
            || value == BattleBuffType.DamageTakenReduce
            || value == BattleBuffType.PhysicalDamageTakenAmplify
            || value == BattleBuffType.PhysicalDamageTakenReduce
            || value == BattleBuffType.MagicalDamageTakenAmplify
            || value == BattleBuffType.MagicalDamageTakenReduce
            || value == BattleBuffType.PureDamageTakenAmplify
            || value == BattleBuffType.PureDamageTakenReduce;

    private static bool RequiresPositiveDuration(BattleBuffType value)
        => value == BattleBuffType.Stun
            || value == BattleBuffType.PhysicalArmorUp
            || value == BattleBuffType.Shield
            || value == BattleBuffType.Thorns
            || value == BattleBuffType.LifeSteal
            || value == BattleBuffType.Rage
            || value == BattleBuffType.SecondWind
            || value == BattleBuffType.DeathBurst
            || value == BattleBuffType.Slow
            || value == BattleBuffType.Silence
            || value == BattleBuffType.PhysicalAttackUp
            || value == BattleBuffType.AttackSpeedUp
            || value == BattleBuffType.EnergyRegenUp
            || value == BattleBuffType.MagicResistUp
            || value == BattleBuffType.PhysicalAttackDown
            || value == BattleBuffType.AttackSpeedDown
            || value == BattleBuffType.EnergyRegenDown
            || value == BattleBuffType.PhysicalArmorDown
            || value == BattleBuffType.MagicResistDown
            || value == BattleBuffType.Taunt
            || value == BattleBuffType.Revive
            || value == BattleBuffType.SummonLife
            || value == BattleBuffType.DamageOverTime
            || value == BattleBuffType.HealOverTime
            || value == BattleBuffType.ControlImmune
            || value == BattleBuffType.SilenceImmune
            || value == BattleBuffType.DisplaceImmune
            || value == BattleBuffType.Uninterruptible
            || value == BattleBuffType.DamageImmune
            || value == BattleBuffType.PhysicalImmune
            || value == BattleBuffType.MagicalImmune
            || value == BattleBuffType.PureImmune
            || value == BattleBuffType.HealingAmplify
            || value == BattleBuffType.HealingReduce
            || value == BattleBuffType.DamageTakenAmplify
            || value == BattleBuffType.DamageTakenReduce
            || value == BattleBuffType.PhysicalDamageTakenAmplify
            || value == BattleBuffType.PhysicalDamageTakenReduce
            || value == BattleBuffType.MagicalDamageTakenAmplify
            || value == BattleBuffType.MagicalDamageTakenReduce
            || value == BattleBuffType.PureDamageTakenAmplify
            || value == BattleBuffType.PureDamageTakenReduce;

    private static bool RequiresPositiveModifierRate(BattleBuffType value)
        => value == BattleBuffType.HealingAmplify
            || value == BattleBuffType.HealingReduce
            || value == BattleBuffType.DamageTakenAmplify
            || value == BattleBuffType.DamageTakenReduce
            || value == BattleBuffType.PhysicalDamageTakenAmplify
            || value == BattleBuffType.PhysicalDamageTakenReduce
            || value == BattleBuffType.MagicalDamageTakenAmplify
            || value == BattleBuffType.MagicalDamageTakenReduce
            || value == BattleBuffType.PureDamageTakenAmplify
            || value == BattleBuffType.PureDamageTakenReduce;

    private static bool IsDefinedPassiveTriggerCondition(BattlePassiveTriggerCondition value)
        => value == BattlePassiveTriggerCondition.HealthBelow
            || value == BattlePassiveTriggerCondition.BattleStart
            || value == BattlePassiveTriggerCondition.KillEnemy
            || value == BattlePassiveTriggerCondition.DamageTaken
            || value == BattlePassiveTriggerCondition.DamageDealt
            || value == BattlePassiveTriggerCondition.AllyDied
            || value == BattlePassiveTriggerCondition.EnemyDied
            || value == BattlePassiveTriggerCondition.SkillStart
            || value == BattlePassiveTriggerCondition.SkillFinish
            || value == BattlePassiveTriggerCondition.HealReceived
            || value == BattlePassiveTriggerCondition.HealGranted
            || value == BattlePassiveTriggerCondition.SkillHit
            || value == BattlePassiveTriggerCondition.AuraTick;

    private static bool IsDefinedDispelMode(BattleDispelMode value)
        => value == BattleDispelMode.Any
            || value == BattleDispelMode.BuffOnly
            || value == BattleDispelMode.DebuffOnly;

    private static bool IsDefinedProjectileMotionType(BattleProjectileMotionType value)
        => value == BattleProjectileMotionType.None
            || value == BattleProjectileMotionType.Homing
            || value == BattleProjectileMotionType.Linear;

    private static bool IsDefinedProjectileImpactType(BattleProjectileImpactType value)
        => value == BattleProjectileImpactType.PrimaryTarget
            || value == BattleProjectileImpactType.AreaAtDestination;

    private static bool IsDefinedProjectileTargetMode(BattleProjectileTargetMode value)
        => value == BattleProjectileTargetMode.PrimaryTarget
            || value == BattleProjectileTargetMode.NearestEnemies
            || value == BattleProjectileTargetMode.RandomEnemies
            || value == BattleProjectileTargetMode.LowestHpEnemies
            || value == BattleProjectileTargetMode.FrontEnemyRow
            || value == BattleProjectileTargetMode.BackEnemyRow
            || value == BattleProjectileTargetMode.NearestAllies
            || value == BattleProjectileTargetMode.RandomAllies
            || value == BattleProjectileTargetMode.LowestHpAllies;
}

