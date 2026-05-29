namespace LeanClr.Battle;

internal static class BattleBuffFactory
{
    public static BattleBuff? Create(in BattleSkillEffectSpec effect)
        => Create(effect, 0);

    public static BattleBuff? Create(in BattleSkillEffectSpec effect, int sourceUnitId)
    {
        return effect.BuffType switch
        {
            BattleBuffType.Stun => new StunBuff(effect.StatusDuration),
            BattleBuffType.PhysicalArmorUp => new PhysicalArmorUpBuff(effect.StatusDuration, effect.Amount),
            BattleBuffType.Shield => new ShieldBuff(effect.StatusDuration, effect.Amount),
            BattleBuffType.Thorns => new ThornsBuff(effect.StatusDuration, effect.Amount),
            BattleBuffType.LifeSteal => new LifeStealBuff(effect.StatusDuration, effect.Amount),
            BattleBuffType.Rage => new RageBuff(effect.StatusDuration, effect.Amount),
            BattleBuffType.SecondWind => new SecondWindBuff(effect.StatusDuration, effect.Amount),
            BattleBuffType.DeathBurst => new DeathBurstBuff(effect.StatusDuration, effect.Amount),
            BattleBuffType.Slow => new SlowBuff(effect.StatusDuration, effect.Amount),
            BattleBuffType.Silence => new SilenceBuff(effect.StatusDuration),
            BattleBuffType.Taunt => new TauntBuff(effect.StatusDuration),
            BattleBuffType.Revive => new ReviveBuff(effect.StatusDuration, effect.Amount),
            BattleBuffType.SummonLife => new SummonLifeBuff(effect.StatusDuration),
            BattleBuffType.DamageOverTime => new PeriodicBuff(BattleBuffType.DamageOverTime, effect.StatusDuration, effect.Amount, LeanClr.Mathematics.Fix32.FromRaw(effect.Value), sourceUnitId),
            BattleBuffType.HealOverTime => new PeriodicBuff(BattleBuffType.HealOverTime, effect.StatusDuration, effect.Amount, LeanClr.Mathematics.Fix32.FromRaw(effect.Value), sourceUnitId),
            BattleBuffType.PhysicalAttackUp => new AttributeModifierBuff(BattleBuffType.PhysicalAttackUp, effect.StatusDuration, BattleAttributeType.PhysicalAttack, effect.Amount),
            BattleBuffType.AttackSpeedUp => new AttributeModifierBuff(BattleBuffType.AttackSpeedUp, effect.StatusDuration, BattleAttributeType.AttackSpeed, effect.Amount),
            BattleBuffType.EnergyRegenUp => new AttributeModifierBuff(BattleBuffType.EnergyRegenUp, effect.StatusDuration, BattleAttributeType.EnergyRegen, effect.Amount),
            BattleBuffType.MagicResistUp => new AttributeModifierBuff(BattleBuffType.MagicResistUp, effect.StatusDuration, BattleAttributeType.MagicResist, effect.Amount),
            BattleBuffType.PhysicalAttackDown => new AttributeModifierBuff(BattleBuffType.PhysicalAttackDown, effect.StatusDuration, BattleAttributeType.PhysicalAttack, -effect.Amount),
            BattleBuffType.AttackSpeedDown => new AttributeModifierBuff(BattleBuffType.AttackSpeedDown, effect.StatusDuration, BattleAttributeType.AttackSpeed, -effect.Amount),
            BattleBuffType.EnergyRegenDown => new AttributeModifierBuff(BattleBuffType.EnergyRegenDown, effect.StatusDuration, BattleAttributeType.EnergyRegen, -effect.Amount),
            BattleBuffType.PhysicalArmorDown => new AttributeModifierBuff(BattleBuffType.PhysicalArmorDown, effect.StatusDuration, BattleAttributeType.PhysicalArmor, -effect.Amount),
            BattleBuffType.MagicResistDown => new AttributeModifierBuff(BattleBuffType.MagicResistDown, effect.StatusDuration, BattleAttributeType.MagicResist, -effect.Amount),
            BattleBuffType.ControlImmune => new BattleFlagBuff(BattleBuffType.ControlImmune, effect.StatusDuration),
            BattleBuffType.SilenceImmune => new BattleFlagBuff(BattleBuffType.SilenceImmune, effect.StatusDuration),
            BattleBuffType.DisplaceImmune => new BattleFlagBuff(BattleBuffType.DisplaceImmune, effect.StatusDuration),
            BattleBuffType.Uninterruptible => new BattleFlagBuff(BattleBuffType.Uninterruptible, effect.StatusDuration),
            BattleBuffType.DamageImmune => new BattleDamageImmunityBuff(BattleBuffType.DamageImmune, effect.StatusDuration),
            BattleBuffType.PhysicalImmune => new BattleDamageImmunityBuff(BattleBuffType.PhysicalImmune, effect.StatusDuration),
            BattleBuffType.MagicalImmune => new BattleDamageImmunityBuff(BattleBuffType.MagicalImmune, effect.StatusDuration),
            BattleBuffType.PureImmune => new BattleDamageImmunityBuff(BattleBuffType.PureImmune, effect.StatusDuration),
            BattleBuffType.HealingAmplify => new BattleDamageModifierBuff(BattleBuffType.HealingAmplify, effect.StatusDuration, effect.Amount, BattleDamageType.All),
            BattleBuffType.HealingReduce => new BattleDamageModifierBuff(BattleBuffType.HealingReduce, effect.StatusDuration, effect.Amount, BattleDamageType.All),
            BattleBuffType.DamageTakenAmplify => new BattleDamageModifierBuff(BattleBuffType.DamageTakenAmplify, effect.StatusDuration, effect.Amount, BattleDamageType.All),
            BattleBuffType.DamageTakenReduce => new BattleDamageModifierBuff(BattleBuffType.DamageTakenReduce, effect.StatusDuration, effect.Amount, BattleDamageType.All),
            BattleBuffType.PhysicalDamageTakenAmplify => new BattleDamageModifierBuff(BattleBuffType.PhysicalDamageTakenAmplify, effect.StatusDuration, effect.Amount, BattleDamageType.Physical),
            BattleBuffType.PhysicalDamageTakenReduce => new BattleDamageModifierBuff(BattleBuffType.PhysicalDamageTakenReduce, effect.StatusDuration, effect.Amount, BattleDamageType.Physical),
            BattleBuffType.MagicalDamageTakenAmplify => new BattleDamageModifierBuff(BattleBuffType.MagicalDamageTakenAmplify, effect.StatusDuration, effect.Amount, BattleDamageType.Magical),
            BattleBuffType.MagicalDamageTakenReduce => new BattleDamageModifierBuff(BattleBuffType.MagicalDamageTakenReduce, effect.StatusDuration, effect.Amount, BattleDamageType.Magical),
            BattleBuffType.PureDamageTakenAmplify => new BattleDamageModifierBuff(BattleBuffType.PureDamageTakenAmplify, effect.StatusDuration, effect.Amount, BattleDamageType.Pure),
            BattleBuffType.PureDamageTakenReduce => new BattleDamageModifierBuff(BattleBuffType.PureDamageTakenReduce, effect.StatusDuration, effect.Amount, BattleDamageType.Pure),
            _ => null,
        };
    }
}
