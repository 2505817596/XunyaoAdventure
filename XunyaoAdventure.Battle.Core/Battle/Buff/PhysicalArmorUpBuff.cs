using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class PhysicalArmorUpBuff : BattleBuff
{
    private int _modifierId;

    public readonly Fix32 PhysicalArmorBonus;

    public PhysicalArmorUpBuff(Fix32 duration, Fix32 physicalArmorBonus) : base(BattleBuffType.PhysicalArmorUp, duration)
    {
        PhysicalArmorBonus = physicalArmorBonus;
    }

    public override void OnApply(BattleUnit unit)
    {
        _modifierId = unit.AddPhysicalArmorModifier(PhysicalArmorBonus);
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
