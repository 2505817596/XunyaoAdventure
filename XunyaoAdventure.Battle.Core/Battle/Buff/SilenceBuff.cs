using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class SilenceBuff : BattleBuff
{
    public SilenceBuff(Fix32 duration) : base(BattleBuffType.Silence, duration)
    {
    }
}
