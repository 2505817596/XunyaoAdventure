using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattlePassiveTriggerResolver
{
    private readonly BattleWorld _world;
    private readonly BattleSkillEffectExecutor _skillEffectExecutor;
    private int _triggerDepth;

    public BattlePassiveTriggerResolver(BattleWorld world, BattleSkillEffectExecutor skillEffectExecutor)
    {
        _world = world;
        _skillEffectExecutor = skillEffectExecutor;
    }

    public void TryTriggerPassiveEffects(BattleUnit unit)
    {
        if (!unit.IsAlive || unit.SkillProfile.PassiveTriggers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < unit.SkillProfile.PassiveTriggers.Length; i++)
        {
            if (unit.IsPassiveTriggerConsumed(i))
            {
                continue;
            }

            BattlePassiveTriggerTemplate trigger = unit.SkillProfile.PassiveTriggers[i];
            if (!ShouldTriggerPassive(unit, trigger))
            {
                continue;
            }

            ExecutePassiveTrigger(unit, unit, i, trigger);
        }
    }

    public void TriggerBattleStartPassives()
    {
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit unit = _world.Units[unitOrder[i]];
            if (!unit.IsAlive || unit.SkillProfile.PassiveTriggers.Length == 0)
            {
                continue;
            }

            for (int j = 0; j < unit.SkillProfile.PassiveTriggers.Length; j++)
            {
                if (unit.IsPassiveTriggerConsumed(j))
                {
                    continue;
                }

                BattlePassiveTriggerTemplate trigger = unit.SkillProfile.PassiveTriggers[j];
                if (trigger.Condition != BattlePassiveTriggerCondition.BattleStart)
                {
                    continue;
                }

                ExecutePassiveTrigger(unit, unit, j, trigger);
            }
        }
    }

    public void TryTriggerKillPassives(BattleUnit killer, BattleUnit deadUnit)
        => TryTriggerUnitPassives(killer, deadUnit, BattlePassiveTriggerCondition.KillEnemy);

    public void TryTriggerDamageDealtPassives(BattleUnit attacker, BattleUnit defender)
        => TryTriggerUnitPassives(attacker, defender, BattlePassiveTriggerCondition.DamageDealt);

    public void TryTriggerDamageTakenPassives(BattleUnit defender, BattleUnit attacker)
        => TryTriggerUnitPassives(defender, attacker, BattlePassiveTriggerCondition.DamageTaken);

    public void TryTriggerHealGrantedPassives(BattleUnit healer, BattleUnit healedUnit)
        => TryTriggerUnitPassives(healer, healedUnit, BattlePassiveTriggerCondition.HealGranted);

    public void TryTriggerHealReceivedPassives(BattleUnit healedUnit, BattleUnit healer)
        => TryTriggerUnitPassives(healedUnit, healer, BattlePassiveTriggerCondition.HealReceived);

    public void TriggerSkillPhasePassives(BattleUnit source, BattleUnit primaryTarget, BattlePassiveTriggerCondition condition)
        => TryTriggerUnitPassives(source, primaryTarget, condition);

    public void TriggerUnitDeathPassives(BattleUnit deadUnit)
    {
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit unit = _world.Units[unitOrder[i]];
            if (!unit.IsAlive || unit.Id == deadUnit.Id || unit.SkillProfile.PassiveTriggers.Length == 0)
            {
                continue;
            }

            BattlePassiveTriggerCondition condition = unit.Team == deadUnit.Team
                ? BattlePassiveTriggerCondition.AllyDied
                : BattlePassiveTriggerCondition.EnemyDied;

            for (int j = 0; j < unit.SkillProfile.PassiveTriggers.Length; j++)
            {
                if (unit.IsPassiveTriggerConsumed(j))
                {
                    continue;
                }

                BattlePassiveTriggerTemplate trigger = unit.SkillProfile.PassiveTriggers[j];
                if (trigger.Condition != condition)
                {
                    continue;
                }

                ExecutePassiveTrigger(unit, deadUnit, j, trigger);
            }
        }
    }

    public void TriggerAuraTickPassives()
    {
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit source = _world.Units[unitOrder[i]];
            if (!source.IsAlive || source.SkillProfile.PassiveTriggers.Length == 0)
            {
                continue;
            }

            for (int j = 0; j < source.SkillProfile.PassiveTriggers.Length; j++)
            {
                if (source.IsPassiveTriggerConsumed(j))
                {
                    continue;
                }

                BattlePassiveTriggerTemplate trigger = source.SkillProfile.PassiveTriggers[j];
                if (trigger.Condition != BattlePassiveTriggerCondition.AuraTick)
                {
                    continue;
                }

                ExecuteAuraPassive(source, j, trigger);
            }
        }
    }

    private void TryTriggerUnitPassives(BattleUnit source, BattleUnit primaryTarget, BattlePassiveTriggerCondition condition)
    {
        if (!source.IsAlive || source.SkillProfile.PassiveTriggers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < source.SkillProfile.PassiveTriggers.Length; i++)
        {
            if (source.IsPassiveTriggerConsumed(i))
            {
                continue;
            }

            BattlePassiveTriggerTemplate trigger = source.SkillProfile.PassiveTriggers[i];
            if (trigger.Condition != condition)
            {
                continue;
            }

            ExecutePassiveTrigger(source, primaryTarget, i, trigger);
        }
    }

    private void ExecutePassiveTrigger(BattleUnit source, BattleUnit primaryTarget, int triggerIndex, in BattlePassiveTriggerTemplate trigger)
    {
        if (_triggerDepth > 0)
        {
            return;
        }

        if (trigger.ConsumeOnTrigger)
        {
            source.ConsumePassiveTrigger(triggerIndex);
        }

        BattleSkillEffectSpec[] effects = BuildPassiveEffects(source, trigger.Effects);
        _triggerDepth++;
        try
        {
            _skillEffectExecutor.ExecuteEffects(source, primaryTarget, effects);
        }
        finally
        {
            _triggerDepth--;
        }
    }

    private void ExecuteAuraPassive(BattleUnit source, int triggerIndex, in BattlePassiveTriggerTemplate trigger)
    {
        if (!source.IsAlive || trigger.Effects.Length == 0 || trigger.Threshold <= Fix32.Zero)
        {
            return;
        }

        if (trigger.ConsumeOnTrigger)
        {
            source.ConsumePassiveTrigger(triggerIndex);
        }

        BattleSkillEffectSpec[] effects = BuildPassiveEffects(source, trigger.Effects);
        _triggerDepth++;
        try
        {
            _skillEffectExecutor.ExecuteEffectsAtPosition(source, source.Position, BuildAuraEffects(effects, trigger.Threshold));
        }
        finally
        {
            _triggerDepth--;
        }
    }

    private static bool ShouldTriggerPassive(BattleUnit unit, in BattlePassiveTriggerTemplate trigger)
    {
        return trigger.Condition switch
        {
            BattlePassiveTriggerCondition.HealthBelow => unit.Spec.MaxHp > Fix32.Zero && unit.Hp <= unit.Spec.MaxHp * trigger.Threshold,
            _ => false,
        };
    }

    private static BattleSkillEffectSpec[] BuildPassiveEffects(BattleUnit unit, BattleSkillEffectTemplate[] templates)
    {
        BattleSkillEffectSpec[] effects = new BattleSkillEffectSpec[templates.Length];
        for (int i = 0; i < templates.Length; i++)
        {
            effects[i] = templates[i].Build(unit.Spec);
        }

        return effects;
    }

    private static BattleSkillEffectSpec[] BuildAuraEffects(BattleSkillEffectSpec[] effects, Fix32 radius)
    {
        BattleSkillEffectSpec[] auraEffects = new BattleSkillEffectSpec[effects.Length];
        for (int i = 0; i < effects.Length; i++)
        {
            BattleSkillEffectSpec effect = effects[i];
            if (effect.Target == BattleSkillEffectTarget.AllyUnitsInRadius || effect.Target == BattleSkillEffectTarget.EnemyUnitsInRadius)
            {
                auraEffects[i] = new BattleSkillEffectSpec(
                    effect.Type,
                    effect.Target,
                    effect.Amount,
                    effect.Radius > Fix32.Zero ? effect.Radius : radius,
                    effect.Value,
                    effect.BuffType,
                    effect.StatusDuration,
                    effect.TargetTeam);
                continue;
            }

            auraEffects[i] = effect;
        }

        return auraEffects;
    }
}
