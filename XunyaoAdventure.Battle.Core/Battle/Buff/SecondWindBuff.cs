using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class SecondWindBuff : TriggeredPhysicalArmorBuff
{
    public SecondWindBuff(Fix32 duration, Fix32 physicalArmorBonusOnHeal) : base(BattleBuffType.SecondWind, duration, physicalArmorBonusOnHeal)
    {
    }

    protected override bool ShouldTrigger(BattleUnit unit, in BattleCombatEvent combatEvent)
    {
        return combatEvent.Type == BattleCombatEventType.Healed
            && combatEvent.TargetUnitId == unit.Id
            && combatEvent.Amount > Fix32.Zero;
    }
}
