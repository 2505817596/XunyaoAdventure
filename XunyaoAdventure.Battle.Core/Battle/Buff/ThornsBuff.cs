using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class ThornsBuff : BattleBuff
{
    public ThornsBuff(Fix32 duration, Fix32 reflectionRate) : base(BattleBuffType.Thorns, duration)
    {
        ReflectionRate = reflectionRate;
    }

    public Fix32 ReflectionRate { get; }
    public override Fix32 Magnitude => ReflectionRate;

    public override Fix32 OnAfterReceiveDamage(BattleUnit unit, BattleUnit source, Fix32 actualDamage)
    {
        if (actualDamage <= Fix32.Zero || ReflectionRate <= Fix32.Zero)
        {
            return Fix32.Zero;
        }

        return actualDamage * ReflectionRate;
    }
}
