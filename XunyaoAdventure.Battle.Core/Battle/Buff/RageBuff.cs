using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class RageBuff : TriggeredPhysicalArmorBuff
{
    public RageBuff(Fix32 duration, Fix32 physicalArmorBonusPerHit) : base(BattleBuffType.Rage, duration, physicalArmorBonusPerHit)
    {
    }

    protected override bool ShouldTrigger(BattleUnit unit, in BattleCombatEvent combatEvent)
    {
        return combatEvent.Type == BattleCombatEventType.DamageTaken
            && combatEvent.TargetUnitId == unit.Id
            && combatEvent.Amount > Fix32.Zero;
    }
}
