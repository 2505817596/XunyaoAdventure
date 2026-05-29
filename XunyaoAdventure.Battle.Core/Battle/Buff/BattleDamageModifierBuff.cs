using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class BattleDamageModifierBuff : BattleBuff
{
    public BattleDamageModifierBuff(BattleBuffType type, Fix32 duration, Fix32 rate, BattleDamageType damageType)
        : base(type, duration)
    {
        Rate = rate;
        DamageType = damageType;
    }

    public Fix32 Rate { get; }
    public BattleDamageType DamageType { get; }
    public override Fix32 Magnitude => Rate;

    public override Fix32 OnBeforeReceiveDamage(BattleUnit unit, BattleUnit source, Fix32 damage, BattleDamageType damageType)
    {
        if (!IsDamageModifierType(Type) || damage <= Fix32.Zero || Rate <= Fix32.Zero)
        {
            return damage;
        }

        if (DamageType != BattleDamageType.All && DamageType != damageType)
        {
            return damage;
        }

        return damage * Rate;
    }

    public override Fix32 OnBeforeReceiveHealing(BattleUnit unit, BattleUnit source, Fix32 healing)
    {
        if (!IsHealingModifierType(Type) || healing <= Fix32.Zero || Rate <= Fix32.Zero)
        {
            return healing;
        }

        return healing * Rate;
    }

    private static bool IsHealingModifierType(BattleBuffType type)
        => type == BattleBuffType.HealingAmplify
            || type == BattleBuffType.HealingReduce;

    private static bool IsDamageModifierType(BattleBuffType type)
        => type == BattleBuffType.DamageTakenAmplify
            || type == BattleBuffType.DamageTakenReduce
            || type == BattleBuffType.PhysicalDamageTakenAmplify
            || type == BattleBuffType.PhysicalDamageTakenReduce
            || type == BattleBuffType.MagicalDamageTakenAmplify
            || type == BattleBuffType.MagicalDamageTakenReduce
            || type == BattleBuffType.PureDamageTakenAmplify
            || type == BattleBuffType.PureDamageTakenReduce;
}
