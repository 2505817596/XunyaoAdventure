using System.Collections.Generic;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class BattleAttributeSet
{
    private const int AttributeCount = 10;

    private readonly List<BattleAttributeModifier> _modifiers = new();
    private readonly Fix32[] _baseValues = new Fix32[AttributeCount];
    private int _nextModifierId = 1;

    public Fix32 MoveSpeed => Resolve(BattleAttributeType.MoveSpeed);
    public Fix32 PhysicalAttack => Resolve(BattleAttributeType.PhysicalAttack);
    public Fix32 AttackSpeed => Resolve(BattleAttributeType.AttackSpeed);
    public Fix32 PhysicalArmor => Resolve(BattleAttributeType.PhysicalArmor);
    public Fix32 MagicResist => Resolve(BattleAttributeType.MagicResist);
    public Fix32 EnergyRegen => Resolve(BattleAttributeType.EnergyRegen);
    public Fix32 MagicPower => Resolve(BattleAttributeType.MagicPower);
    public Fix32 PhysicalCrit => Resolve(BattleAttributeType.PhysicalCrit);
    public Fix32 HpRegen => Resolve(BattleAttributeType.HpRegen);
    public Fix32 InterruptThreshold => Resolve(BattleAttributeType.InterruptThreshold);

    public void SetBase(BattleAttributeType type, Fix32 value)
    {
        _baseValues[(int)type] = value > Fix32.Zero ? value : Fix32.Zero;
    }

    public int AddFlat(BattleAttributeType type, Fix32 value)
    {
        if (value == Fix32.Zero)
        {
            return 0;
        }

        int id = _nextModifierId++;
        _modifiers.Add(new BattleAttributeModifier(id, type, BattleAttributeModifierOp.FlatAdd, value));
        return id;
    }

    public bool Remove(int modifierId)
    {
        if (modifierId == 0)
        {
            return false;
        }

        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (_modifiers[i].Id != modifierId)
            {
                continue;
            }

            _modifiers.RemoveAt(i);
            return true;
        }

        return false;
    }

    private Fix32 Resolve(BattleAttributeType type)
    {
        Fix32 value = _baseValues[(int)type];
        for (int i = 0; i < _modifiers.Count; i++)
        {
            BattleAttributeModifier modifier = _modifiers[i];
            if (modifier.Type != type || modifier.Op != BattleAttributeModifierOp.FlatAdd)
            {
                continue;
            }

            value += modifier.Value;
        }

        if (value < Fix32.Zero)
        {
            value = Fix32.Zero;
        }

        return value;
    }
}
