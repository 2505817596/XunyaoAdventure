using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class ReviveBuff : BattleBuff
{
    public ReviveBuff(Fix32 duration, Fix32 reviveRatio) : base(BattleBuffType.Revive, duration)
    {
        ReviveRatio = reviveRatio;
    }

    public Fix32 ReviveRatio { get; }
    public override Fix32 Magnitude => ReviveRatio;
}
