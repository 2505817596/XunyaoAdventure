using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleUnit
{
    internal int AddMoveSpeedModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.MoveSpeed, amount);

    internal int AddPhysicalAttackModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.PhysicalAttack, amount);

    internal int AddAttackSpeedModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.AttackSpeed, amount);

    internal int AddPhysicalArmorModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.PhysicalArmor, amount);

    internal int AddMagicResistModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.MagicResist, amount);

    internal int AddEnergyRegenModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.EnergyRegen, amount);

    internal int AddMagicPowerModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.MagicPower, amount);

    internal int AddPhysicalCritModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.PhysicalCrit, amount);

    internal int AddHpRegenModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.HpRegen, amount);

    internal int AddInterruptThresholdModifier(Fix32 amount)
        => _attributes.AddFlat(BattleAttributeType.InterruptThreshold, amount);

    internal bool RemoveAttributeModifier(int modifierId)
        => _attributes.Remove(modifierId);

    private int FindBuffIndex(BattleBuffType type)
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            if (_buffs[i].Buff.Type == type)
            {
                return i;
            }
        }

        return -1;
    }

    private int CountBuffStacks(BattleBuffType type)
    {
        int count = 0;
        for (int i = 0; i < _buffs.Count; i++)
        {
            if (_buffs[i].Buff.Type == type)
            {
                count++;
            }
        }

        return count;
    }

    private void RemoveAllBuffs()
    {
        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            _buffs[i].Buff.OnRemove(this);
        }

        _buffs.Clear();
    }

    private bool TryConsumeReviveBuff(out Fix32 reviveHp)
    {
        reviveHp = Fix32.Zero;
        int index = FindBuffIndex(BattleBuffType.Revive);
        if (index < 0)
        {
            return false;
        }

        BattleBuffInstance instance = _buffs[index];
        if (instance.Buff is not ReviveBuff reviveBuff)
        {
            return false;
        }

        reviveHp = Spec.MaxHp * reviveBuff.ReviveRatio;
        if (reviveHp <= Fix32.Zero)
        {
            return false;
        }

        instance.Buff.OnRemove(this);
        _buffs.RemoveAt(index);
        return true;
    }
}

