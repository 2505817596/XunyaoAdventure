using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class SummonLifeBuff : BattleBuff
{
    public SummonLifeBuff(Fix32 duration) : base(BattleBuffType.SummonLife, duration)
    {
    }
}
