using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class AttributeModifierBuff : BattleBuff
{
    private int _modifierId;

    public AttributeModifierBuff(BattleBuffType type, Fix32 duration, BattleAttributeType attributeType, Fix32 amount)
        : base(type, duration)
    {
        AttributeType = attributeType;
        Amount = amount;
    }

    public BattleAttributeType AttributeType { get; }
    public Fix32 Amount { get; }
    public override Fix32 Magnitude => Amount;

    public override void OnApply(BattleUnit unit)
    {
        _modifierId = AddModifier(unit);
    }

    public override void OnExpire(BattleUnit unit)
    {
        RemoveModifier(unit);
    }

    public override void OnRemove(BattleUnit unit)
    {
        RemoveModifier(unit);
    }

    private int AddModifier(BattleUnit unit)
    {
        return AttributeType switch
        {
            BattleAttributeType.MoveSpeed => unit.AddMoveSpeedModifier(Amount),
            BattleAttributeType.PhysicalAttack => unit.AddPhysicalAttackModifier(Amount),
            BattleAttributeType.AttackSpeed => unit.AddAttackSpeedModifier(Amount),
            BattleAttributeType.PhysicalArmor => unit.AddPhysicalArmorModifier(Amount),
            BattleAttributeType.MagicResist => unit.AddMagicResistModifier(Amount),
            BattleAttributeType.EnergyRegen => unit.AddEnergyRegenModifier(Amount),
            BattleAttributeType.MagicPower => unit.AddMagicPowerModifier(Amount),
            BattleAttributeType.PhysicalCrit => unit.AddPhysicalCritModifier(Amount),
            BattleAttributeType.HpRegen => unit.AddHpRegenModifier(Amount),
            BattleAttributeType.InterruptThreshold => unit.AddInterruptThresholdModifier(Amount),
            _ => 0,
        };
    }

    private void RemoveModifier(BattleUnit unit)
    {
        unit.RemoveAttributeModifier(_modifierId);
        _modifierId = 0;
    }
}
