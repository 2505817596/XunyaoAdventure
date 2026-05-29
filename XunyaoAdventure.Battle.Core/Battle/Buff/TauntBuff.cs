using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class TauntBuff : BattleBuff
{
    public TauntBuff(Fix32 duration) : base(BattleBuffType.Taunt, duration)
    {
    }
}
