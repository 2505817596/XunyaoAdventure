using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class SlowBuff : BattleBuff
{
    private int _modifierId;

    public SlowBuff(Fix32 duration, Fix32 moveSpeedReduction) : base(BattleBuffType.Slow, duration)
    {
        MoveSpeedReduction = moveSpeedReduction;
    }

    public Fix32 MoveSpeedReduction { get; }
    public override Fix32 Magnitude => MoveSpeedReduction;

    public override void OnApply(BattleUnit unit)
    {
        _modifierId = unit.AddMoveSpeedModifier(-MoveSpeedReduction);
    }

    public override void OnExpire(BattleUnit unit)
    {
        RemoveModifier(unit);
    }

    public override void OnRemove(BattleUnit unit)
    {
        RemoveModifier(unit);
    }

    private void RemoveModifier(BattleUnit unit)
    {
        unit.RemoveAttributeModifier(_modifierId);
        _modifierId = 0;
    }
}
