using LeanClr.Mathematics;

namespace LeanClr.Battle;

public abstract class TriggeredPhysicalArmorBuff : BattleEventTriggeredBuff
{
    private readonly System.Collections.Generic.List<int> _modifierIds = new();

    protected TriggeredPhysicalArmorBuff(BattleBuffType type, Fix32 duration, Fix32 physicalArmorBonusPerTrigger) : base(type, duration)
    {
        PhysicalArmorBonusPerTrigger = physicalArmorBonusPerTrigger;
    }

    public Fix32 PhysicalArmorBonusPerTrigger { get; }
    public override Fix32 Magnitude => PhysicalArmorBonusPerTrigger;

    protected sealed override Fix32 OnTriggered(BattleUnit unit, in BattleCombatEvent combatEvent)
    {
        if (PhysicalArmorBonusPerTrigger <= Fix32.Zero)
        {
            return Fix32.Zero;
        }

        int modifierId = unit.AddPhysicalArmorModifier(PhysicalArmorBonusPerTrigger);
        if (modifierId != 0)
        {
            _modifierIds.Add(modifierId);
        }
        return PhysicalArmorBonusPerTrigger;
    }

    public override void OnExpire(BattleUnit unit)
    {
        RemoveAccumulatedPhysicalArmor(unit);
    }

    public override void OnRemove(BattleUnit unit)
    {
        RemoveAccumulatedPhysicalArmor(unit);
    }

    private void RemoveAccumulatedPhysicalArmor(BattleUnit unit)
    {
        for (int i = _modifierIds.Count - 1; i >= 0; i--)
        {
            unit.RemoveAttributeModifier(_modifierIds[i]);
        }

        _modifierIds.Clear();
    }
}
