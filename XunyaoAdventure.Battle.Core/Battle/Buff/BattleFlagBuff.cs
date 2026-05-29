using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class BattleFlagBuff : BattleBuff
{
    public BattleFlagBuff(BattleBuffType type, Fix32 duration) : base(type, duration)
    {
    }
}
