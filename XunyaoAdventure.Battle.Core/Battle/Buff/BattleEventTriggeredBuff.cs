using LeanClr.Mathematics;

namespace LeanClr.Battle;

public abstract class BattleEventTriggeredBuff : BattleBuff
{
    protected BattleEventTriggeredBuff(BattleBuffType type, Fix32 duration) : base(type, duration)
    {
    }

    public sealed override Fix32 OnCombatEvent(BattleUnit unit, in BattleCombatEvent combatEvent)
    {
        if (!ShouldTrigger(unit, combatEvent))
        {
            return Fix32.Zero;
        }

        return OnTriggered(unit, combatEvent);
    }

    protected abstract bool ShouldTrigger(BattleUnit unit, in BattleCombatEvent combatEvent);
    protected abstract Fix32 OnTriggered(BattleUnit unit, in BattleCombatEvent combatEvent);
}
