using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class BattleSkillRuntime
{
    private int _nextEventIndex;

    public BattleSkillRuntime(BattleSkillSpec spec, int targetUnitId)
    {
        Spec = spec;
        TargetUnitId = targetUnitId;
        Phase = BattleSkillRuntimePhase.Windup;
        ElapsedTime = Fix32.Zero;
        RemainingTime = spec.WindupTime + spec.RecoverTime;
        _nextEventIndex = 0;
    }

    public BattleSkillSpec Spec { get; }
    public int TargetUnitId { get; }
    public BattleSkillRuntimePhase Phase { get; private set; }
    public Fix32 RemainingTime { get; private set; }
    public Fix32 ElapsedTime { get; private set; }
    public bool IsFinished => Phase == BattleSkillRuntimePhase.Finished;

    public void Tick(Fix32 deltaTime)
    {
        if (Phase == BattleSkillRuntimePhase.Finished || Phase == BattleSkillRuntimePhase.None)
        {
            return;
        }

        ElapsedTime += deltaTime;

        if (ElapsedTime >= Spec.WindupTime)
        {
            Phase = BattleSkillRuntimePhase.Recover;
        }

        if (ElapsedTime >= Spec.WindupTime + Spec.RecoverTime)
        {
            ElapsedTime = Spec.WindupTime + Spec.RecoverTime;
            RemainingTime = Fix32.Zero;
            Phase = BattleSkillRuntimePhase.Finished;
        }
        else
        {
            RemainingTime = Spec.WindupTime + Spec.RecoverTime - ElapsedTime;
        }
    }

    public bool TryConsumeHit(out BattleSkillHit hit)
    {
        hit = default;
        BattleSkillTimelineEvent[] events = Spec.TimelineEvents;
        if (_nextEventIndex >= events.Length)
        {
            return false;
        }

        BattleSkillTimelineEvent timelineEvent = events[_nextEventIndex];
        if (timelineEvent.Time > ElapsedTime)
        {
            return false;
        }

        _nextEventIndex++;
        hit = new BattleSkillHit(TargetUnitId, Spec, timelineEvent);
        return true;
    }
}
