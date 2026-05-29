using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class DeathBurstBuff : TriggeredPhysicalArmorBuff
{
    public DeathBurstBuff(Fix32 duration, Fix32 physicalArmorBonusOnDeath) : base(BattleBuffType.DeathBurst, duration, physicalArmorBonusOnDeath)
    {
    }

    protected override bool ShouldTrigger(BattleUnit unit, in BattleCombatEvent combatEvent)
    {
        return combatEvent.Type == BattleCombatEventType.UnitDied
            && combatEvent.SourceUnitId == unit.Id
            && combatEvent.TargetUnitId != unit.Id;
    }
}
