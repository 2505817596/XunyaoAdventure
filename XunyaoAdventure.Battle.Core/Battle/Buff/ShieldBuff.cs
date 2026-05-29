using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class ShieldBuff : BattleBuff
{
    public ShieldBuff(Fix32 duration, Fix32 shieldAmount) : base(BattleBuffType.Shield, duration)
    {
        RemainingShield = shieldAmount;
    }

    public Fix32 RemainingShield { get; private set; }
    public override Fix32 Magnitude => RemainingShield;
    public override int BeforeReceiveDamagePriority => -100;

    public override Fix32 OnBeforeReceiveDamage(BattleUnit unit, BattleUnit source, Fix32 damage, BattleDamageType damageType)
    {
        if (RemainingShield <= Fix32.Zero || damage <= Fix32.Zero)
        {
            return damage;
        }

        Fix32 absorbed = damage;
        if (absorbed > RemainingShield)
        {
            absorbed = RemainingShield;
        }

        RemainingShield -= absorbed;
        if (RemainingShield < Fix32.Zero)
        {
            RemainingShield = Fix32.Zero;
        }

        Fix32 reduced = damage - absorbed;
        if (reduced < Fix32.Zero)
        {
            reduced = Fix32.Zero;
        }

        return reduced;
    }

    public override bool IsConsumed => RemainingShield <= Fix32.Zero;
}
