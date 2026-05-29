using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleUnit
{
    internal void BeginSkill(BattleSkillSpec skill, BattleUnit target)
    {
        CurrentTargetId = target.Id;
        CurrentSkill = new BattleSkillRuntime(CreateRuntimeSkillSpec(skill), target.Id);
        ActionState = BattleUnitActionState.SkillWindup;
        SetFacing(target.Position - Position);
    }

    internal void TickSkill(Fix32 deltaTime)
    {
        if (!IsAlive || CurrentSkill == null)
        {
            return;
        }

        CurrentSkill.Tick(deltaTime);
        RefreshSkillActionState();
    }

    internal bool TryConsumeSkillHit(out BattleSkillHit hit)
    {
        hit = default;
        if (!IsAlive || CurrentSkill == null)
        {
            return false;
        }

        return CurrentSkill.TryConsumeHit(out hit);
    }

    internal void RefreshSkillActionState()
    {
        if (CurrentSkill == null)
        {
            return;
        }

        if (CurrentSkill.IsFinished)
        {
            LastFinishedSkillType = CurrentSkill.Spec.Type;
            CurrentSkill = null;
            ActionState = BattleUnitActionState.Idle;
        }
        else if (CurrentSkill.Phase == BattleSkillRuntimePhase.Recover)
        {
            ActionState = BattleUnitActionState.SkillRecover;
        }
        else
        {
            ActionState = BattleUnitActionState.SkillWindup;
        }
    }

    internal void GainEnergy(Fix32 amount)
    {
        Energy += amount;
        if (Energy > Fix32.One)
        {
            Energy = Fix32.One;
        }
    }

    internal void ModifyEnergy(Fix32 amount)
    {
        if (amount >= Fix32.Zero)
        {
            GainEnergy(amount);
            return;
        }

        Energy += amount;
        if (Energy < Fix32.Zero)
        {
            Energy = Fix32.Zero;
        }
    }
}

