using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class LifeStealBuff : BattleBuff
{
    public LifeStealBuff(Fix32 duration, Fix32 lifeStealRate) : base(BattleBuffType.LifeSteal, duration)
    {
        LifeStealRate = lifeStealRate;
    }

    public Fix32 LifeStealRate { get; }
    public override Fix32 Magnitude => LifeStealRate;

    public override Fix32 OnAfterDealDamage(BattleUnit unit, BattleUnit target, Fix32 actualDamage)
    {
        if (actualDamage <= Fix32.Zero || LifeStealRate <= Fix32.Zero)
        {
            return Fix32.Zero;
        }

        return actualDamage * LifeStealRate;
    }
}
