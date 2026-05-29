using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleSkillCastController
{
    private static readonly Fix32 RangedBasicAttackThreshold = Fix32.FromDouble(2.0);

    private readonly BattleWorld _world;
    private readonly BattleSkillEffectExecutor _skillEffectExecutor;

    public BattleSkillCastController(BattleWorld world, BattleSkillEffectExecutor skillEffectExecutor)
    {
        _world = world;
        _skillEffectExecutor = skillEffectExecutor;
    }

    public void ProcessUnit(BattleUnit unit, Fix32 deltaTime)
    {
        if (unit.IsBusy)
        {
            ProcessUnitAction(unit, deltaTime);
            return;
        }

        BattleUnit? target = _world.ResolveTarget(unit);
        if (target == null)
        {
            return;
        }

        if (TryBeginAutoSkill(unit, target, BattleSkillSlot.AutoSkill1) || TryBeginAutoSkill(unit, target, BattleSkillSlot.AutoSkill2))
        {
            return;
        }

        BattleSkillSpec basicAttack = unit.Skills.GetSkill(BattleSkillSlot.BasicAttack);
        Fix32 attackRange = basicAttack.Range + unit.Spec.Radius + target.Spec.Radius;
        if (!IsBasicAttackInRange(unit, target, basicAttack, attackRange))
        {
            FixVec2 chaseTarget = new(target.Position.X, unit.Position.Y);
            unit.MoveAxisFirstTowards(chaseTarget, attackRange, deltaTime, out bool moved);
            if (moved)
            {
                _world.AddBattleEvent(BattleEvent.UnitMoved(unit.Id, unit.Position, unit.Team));
            }

            return;
        }

        unit.BeginSkill(basicAttack, target);
        _world.TriggerSkillPhasePassives(unit, target, BattlePassiveTriggerCondition.SkillStart);
        _world.AddBattleEvent(BattleEvent.UnitAttack(unit.Id, target.Id, basicAttack.Damage, unit.Team));
    }

    private static bool IsBasicAttackInRange(BattleUnit unit, BattleUnit target, BattleSkillSpec basicAttack, Fix32 attackRange)
        => Fix32.Abs(target.Position.X - unit.Position.X) <= attackRange;

    private static bool IsRangedBasicAttack(BattleSkillSpec basicAttack)
        => basicAttack.Range >= RangedBasicAttackThreshold;

    public void BeginUltimate(BattleUnit caster, BattleUnit target)
    {
        caster.CancelAction();
        BattleSkillSpec ultimate = caster.Skills.GetSkill(BattleSkillSlot.Ultimate);
        caster.ConsumeSkillCost(ultimate);
        _world.TriggerSkillPhasePassives(caster, target, BattlePassiveTriggerCondition.SkillStart);
        caster.BeginSkill(ultimate, target);
        _world.AddBattleEvent(BattleEvent.UltimateCast(caster.Id, target.Id, ultimate.Damage, caster.Team));
    }

    public void ExecuteResolvedSkillHit(BattleUnit unit, BattleUnit target, BattleSkillHit hit)
    {
        if (hit.Skill.Type == BattleSkillType.Ultimate)
        {
            _world.AddBattleEvent(BattleEvent.UltimateHit(unit.Id, target.Id, hit.Skill.Damage, unit.Team));
        }

        _skillEffectExecutor.Execute(unit, target, hit);
        _world.TriggerSkillPhasePassives(unit, target, BattlePassiveTriggerCondition.SkillHit);
    }

    public void ExecuteResolvedSkillHitAtPosition(BattleUnit unit, FixVec2 position, BattleSkillHit hit)
    {
        if (hit.Skill.Type == BattleSkillType.Ultimate)
        {
            _world.AddBattleEvent(BattleEvent.UltimateHit(unit.Id, hit.TargetUnitId, hit.Skill.Damage, unit.Team));
        }

        _skillEffectExecutor.ExecuteAtPosition(unit, position, hit);
        if (_world.TryGetUnit(hit.TargetUnitId, out BattleUnit target) && target.IsAlive)
        {
            _world.TriggerSkillPhasePassives(unit, target, BattlePassiveTriggerCondition.SkillHit);
        }
        else
        {
            _world.TriggerSkillPhasePassives(unit, unit, BattlePassiveTriggerCondition.SkillHit);
        }
    }

    private bool TryBeginAutoSkill(BattleUnit unit, BattleUnit fallbackTarget, BattleSkillSlot slot)
    {
        if (!unit.CanCastAutoSkill(slot))
        {
            return false;
        }

        BattleSkillSpec skill = unit.Skills.GetSkill(slot);
        BattleUnit? target = _world.ResolveSkillTarget(unit, skill, fallbackTarget.Id);
        if (target == null)
        {
            return false;
        }

        if (skill.RequiresRangeCheck && FixVec2.Distance(unit.Position, target.Position) > skill.Range + unit.Spec.Radius + target.Spec.Radius)
        {
            return false;
        }

        unit.ConsumeSkillCost(skill);
        _world.TriggerSkillPhasePassives(unit, target, BattlePassiveTriggerCondition.SkillStart);
        unit.BeginSkill(skill, target);
        _world.AddBattleEvent(BattleEvent.UnitAttack(unit.Id, target.Id, skill.Damage, unit.Team));
        return true;
    }

    private void ProcessUnitAction(BattleUnit unit, Fix32 deltaTime)
    {
        unit.TickSkill(deltaTime);

        bool hasHit = false;
        while (unit.TryConsumeSkillHit(out BattleSkillHit hit))
        {
            hasHit = true;
            ProcessSkillHit(unit, hit);
            if (!unit.IsAlive)
            {
                break;
            }
        }

        if (!hasHit)
        {
            EmitSkillRecoveredIfFinished(unit);
            return;
        }

        unit.RefreshSkillActionState();
        EmitSkillRecoveredIfFinished(unit);
    }

    private void ProcessSkillHit(BattleUnit unit, BattleSkillHit hit)
    {
        if (!_world.TryGetUnit(hit.TargetUnitId, out BattleUnit target) || !target.IsAlive)
        {
            return;
        }

        if (hit.Skill.RequiresRangeCheck && !IsSkillHitInRange(unit, target, hit.Skill))
        {
            return;
        }

        if (hit.EnergyGain > Fix32.Zero)
        {
            unit.GainEnergy(hit.EnergyGain);
        }

        if (hit.ProjectileSpeed > Fix32.Zero && hit.ProjectileMotionType != BattleProjectileMotionType.None)
        {
            _world.SpawnProjectiles(unit, target, hit);
            return;
        }

        ExecuteResolvedSkillHit(unit, target, hit);
    }

    private static bool IsSkillHitInRange(BattleUnit unit, BattleUnit target, BattleSkillSpec skill)
    {
        Fix32 range = skill.Range + unit.Spec.Radius + target.Spec.Radius;
        if (skill.Slot == BattleSkillSlot.BasicAttack)
        {
            return IsBasicAttackInRange(unit, target, skill, range);
        }

        return FixVec2.Distance(unit.Position, target.Position) <= range;
    }

    private void EmitSkillRecoveredIfFinished(BattleUnit unit)
    {
        if (unit.ActionState == BattleUnitActionState.Idle && unit.LastFinishedSkillType == BattleSkillType.Ultimate)
        {
            _world.AddBattleEvent(BattleEvent.UltimateRecovered(unit.Id, unit.CurrentTargetId, unit.Team));
        }

        if (unit.ActionState == BattleUnitActionState.Idle && unit.LastFinishedSkillType != BattleSkillType.None)
        {
            _world.TriggerSkillPhasePassives(unit, unit, BattlePassiveTriggerCondition.SkillFinish);
        }

        if (unit.LastFinishedSkillType != BattleSkillType.None)
        {
            unit.ClearLastFinishedSkill();
        }
    }
}
