using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class StunBuff : BattleBuff
{
    public StunBuff(Fix32 duration) : base(BattleBuffType.Stun, duration) { }

    public override bool BlocksControl => true;

    public override void OnApply(BattleUnit unit)
    {
        unit.CancelAction();
    }
}
