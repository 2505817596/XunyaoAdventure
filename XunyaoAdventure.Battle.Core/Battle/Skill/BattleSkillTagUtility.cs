namespace LeanClr.Battle;

internal static class BattleSkillTagUtility
{
    public static BattleSkillTag InferEffectTags(BattleSkillEffectType effectType, BattleBuffType buffType)
    {
        BattleSkillTag tags = effectType switch
        {
            BattleSkillEffectType.Damage => BattleSkillTag.Damage,
            BattleSkillEffectType.Heal => BattleSkillTag.Heal,
            BattleSkillEffectType.ApplyStatus => BattleSkillTag.Buff,
            BattleSkillEffectType.Dispel => BattleSkillTag.Dispel,
            BattleSkillEffectType.ModifyEnergy => BattleSkillTag.Energy,
            BattleSkillEffectType.Displace => BattleSkillTag.Displace | BattleSkillTag.Control,
            BattleSkillEffectType.Revive => BattleSkillTag.Revive,
            BattleSkillEffectType.Summon => BattleSkillTag.Summon,
            BattleSkillEffectType.Execute => BattleSkillTag.Damage | BattleSkillTag.NoLifeSteal | BattleSkillTag.NoReflection,
            BattleSkillEffectType.Interrupt => BattleSkillTag.Control,
            _ => BattleSkillTag.None,
        };

        return tags | InferBuffTags(buffType);
    }

    public static BattleSkillTag InferBuffTags(BattleBuffType buffType)
    {
        return buffType switch
        {
            BattleBuffType.Stun => BattleSkillTag.Debuff | BattleSkillTag.Control,
            BattleBuffType.Silence => BattleSkillTag.Debuff | BattleSkillTag.Control,
            BattleBuffType.Taunt => BattleSkillTag.Debuff | BattleSkillTag.Control,
            BattleBuffType.Slow => BattleSkillTag.Debuff,
            BattleBuffType.PhysicalAttackDown => BattleSkillTag.Debuff,
            BattleBuffType.AttackSpeedDown => BattleSkillTag.Debuff,
            BattleBuffType.EnergyRegenDown => BattleSkillTag.Debuff,
            BattleBuffType.PhysicalArmorDown => BattleSkillTag.Debuff,
            BattleBuffType.MagicResistDown => BattleSkillTag.Debuff,
            BattleBuffType.Shield => BattleSkillTag.Buff | BattleSkillTag.Shield,
            BattleBuffType.DamageOverTime => BattleSkillTag.Debuff | BattleSkillTag.Damage | BattleSkillTag.Periodic | BattleSkillTag.NoLifeSteal | BattleSkillTag.NoReflection,
            BattleBuffType.HealOverTime => BattleSkillTag.Buff | BattleSkillTag.Heal | BattleSkillTag.Periodic,
            BattleBuffType.Revive => BattleSkillTag.Buff | BattleSkillTag.Revive,
            BattleBuffType.SummonLife => BattleSkillTag.Buff | BattleSkillTag.Summon,
            BattleBuffType.ControlImmune => BattleSkillTag.Buff | BattleSkillTag.Immunity,
            BattleBuffType.SilenceImmune => BattleSkillTag.Buff | BattleSkillTag.Immunity,
            BattleBuffType.DisplaceImmune => BattleSkillTag.Buff | BattleSkillTag.Immunity,
            BattleBuffType.Uninterruptible => BattleSkillTag.Buff | BattleSkillTag.Immunity,
            BattleBuffType.DamageImmune => BattleSkillTag.Buff | BattleSkillTag.Immunity,
            BattleBuffType.PhysicalImmune => BattleSkillTag.Buff | BattleSkillTag.Immunity,
            BattleBuffType.MagicalImmune => BattleSkillTag.Buff | BattleSkillTag.Immunity,
            BattleBuffType.PureImmune => BattleSkillTag.Buff | BattleSkillTag.Immunity,
            BattleBuffType.HealingAmplify => BattleSkillTag.Buff,
            BattleBuffType.HealingReduce => BattleSkillTag.Debuff,
            BattleBuffType.DamageTakenAmplify => BattleSkillTag.Debuff,
            BattleBuffType.DamageTakenReduce => BattleSkillTag.Buff,
            BattleBuffType.PhysicalDamageTakenAmplify => BattleSkillTag.Debuff,
            BattleBuffType.PhysicalDamageTakenReduce => BattleSkillTag.Buff,
            BattleBuffType.MagicalDamageTakenAmplify => BattleSkillTag.Debuff,
            BattleBuffType.MagicalDamageTakenReduce => BattleSkillTag.Buff,
            BattleBuffType.PureDamageTakenAmplify => BattleSkillTag.Debuff,
            BattleBuffType.PureDamageTakenReduce => BattleSkillTag.Buff,
            BattleBuffType.None => BattleSkillTag.None,
            _ => BattleSkillTag.Buff,
        };
    }

    public static bool Has(BattleSkillTag tags, BattleSkillTag value)
        => (tags & value) == value;
}
