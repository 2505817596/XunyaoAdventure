using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleWorld
{
    internal void TriggerSkillPhasePassives(BattleUnit source, BattleUnit primaryTarget, BattlePassiveTriggerCondition condition)
        => _passiveTriggerResolver.TriggerSkillPhasePassives(source, primaryTarget, condition);

    internal void ApplyBuff(BattleUnit source, BattleUnit target, BattleBuff buff)
        => _effectResolver.ApplyBuff(source, target, buff);

    internal void ApplyDispel(BattleUnit source, BattleUnit target, int maxCount)
        => _effectResolver.ApplyDispel(source, target, maxCount);

    internal void ApplyDispel(BattleUnit source, BattleUnit target, int maxCount, BattleDispelMode mode)
        => _effectResolver.ApplyDispel(source, target, maxCount, mode);

    internal void ApplyHeal(BattleUnit source, BattleUnit target, Fix32 heal)
        => _effectResolver.ApplyHeal(source, target, heal);

    internal void ApplyEnergy(BattleUnit source, BattleUnit target, Fix32 amount)
        => _effectResolver.ApplyEnergy(source, target, amount);

    internal void ApplyDisplace(BattleUnit source, BattleUnit target, Fix32 distance)
        => _effectResolver.ApplyDisplace(source, target, distance);

    internal void ApplyInterrupt(BattleUnit source, BattleUnit target)
        => _effectResolver.ApplyInterrupt(source, target);

    internal void ApplySummon(BattleUnit source, BattleUnit target, BattleSkillEffectSpec effect)
    {
        if (!source.IsAlive)
        {
            return;
        }

        BattleUnit? anchor = target.IsAlive ? target : source;
        _unitLifecycleController.QueueSummon(source, anchor.Position, effect.Value, effect.Amount, effect.StatusDuration);
    }

    internal void ApplySummonAtPosition(BattleUnit source, FixVec2 center, BattleSkillEffectSpec effect)
    {
        if (!source.IsAlive)
        {
            return;
        }

        _unitLifecycleController.QueueSummon(source, center, effect.Value, effect.Amount, effect.StatusDuration);
    }

    internal void ApplyDamage(BattleUnit source, BattleUnit target, Fix32 damage)
        => ApplyDamage(source, target, damage, BattleDamageType.Physical);

    internal void ApplyDamage(BattleUnit source, BattleUnit target, Fix32 damage, BattleDamageType damageType)
        => ApplyDamage(source, target, damage, damageType, BattleSkillType.BasicAttack);

    internal void ApplyDamage(BattleUnit source, BattleUnit target, Fix32 damage, BattleDamageType damageType, BattleSkillType skillType)
        => _damageResolver.ApplyDamage(source, target, damage, damageType, skillType);

    internal void ApplyDamage(BattleUnit source, BattleUnit target, Fix32 damage, BattleDamageType damageType, BattleSkillType skillType, BattleSkillTag tags)
        => _damageResolver.ApplyDamage(source, target, damage, damageType, skillType, tags);

    internal void TryTriggerPassiveEffects(BattleUnit unit)
        => _passiveTriggerResolver.TryTriggerPassiveEffects(unit);

    internal void TryTriggerKillPassives(BattleUnit killer, BattleUnit deadUnit)
        => _passiveTriggerResolver.TryTriggerKillPassives(killer, deadUnit);

    internal void TryTriggerDamageDealtPassives(BattleUnit attacker, BattleUnit defender)
        => _passiveTriggerResolver.TryTriggerDamageDealtPassives(attacker, defender);

    internal void TryTriggerDamageTakenPassives(BattleUnit defender, BattleUnit attacker)
        => _passiveTriggerResolver.TryTriggerDamageTakenPassives(defender, attacker);

    internal void TryTriggerHealGrantedPassives(BattleUnit healer, BattleUnit healedUnit)
        => _passiveTriggerResolver.TryTriggerHealGrantedPassives(healer, healedUnit);

    internal void TryTriggerHealReceivedPassives(BattleUnit healedUnit, BattleUnit healer)
        => _passiveTriggerResolver.TryTriggerHealReceivedPassives(healedUnit, healer);

    internal void TriggerUnitDeathPassives(BattleUnit deadUnit)
        => _passiveTriggerResolver.TriggerUnitDeathPassives(deadUnit);

    internal BattleUnit ResolveEffectSource(int sourceUnitId, BattleUnit fallback)
    {
        if (_units.TryGetValue(sourceUnitId, out BattleUnit source) && source.IsAlive)
        {
            return source;
        }

        return fallback;
    }

    internal void ApplyPeriodicDamage(BattleUnit source, BattleUnit target, Fix32 damage, BattleBuffType buffType)
        => _damageResolver.ApplyPeriodicDamage(source, target, damage, buffType);

    internal void ApplyPeriodicHeal(BattleUnit source, BattleUnit target, Fix32 heal, BattleBuffType buffType)
        => _effectResolver.ApplyPeriodicHeal(source, target, heal, buffType);

    internal void AddBattleEvent(BattleEvent battleEvent)
        => _events.Add(battleEvent);

    internal void DispatchCombatEvent(in BattleCombatEvent combatEvent)
    {
        _combatEventTriggers.Clear();
        for (int i = 0; i < _unitOrder.Count; i++)
        {
            BattleUnit unit = _units[_unitOrder[i]];
            if (!unit.IsAlive)
            {
                continue;
            }

            unit.NotifyCombatEvent(combatEvent, _combatEventTriggers);
        }

        for (int i = 0; i < _combatEventTriggers.Count; i++)
        {
            BattleBuffTrigger trigger = _combatEventTriggers[i];
            _events.Add(BattleEvent.BuffTriggered(trigger.UnitId, combatEvent.SourceUnitId, trigger.BuffType, trigger.Amount, trigger.Team));
        }
    }

    internal void HandleUnitDied(BattleUnit deadUnit, BattleUnit source)
        => _unitLifecycleController.HandleUnitDied(deadUnit, source);

    internal void FlushPendingDeathEffects()
        => _unitLifecycleController.FlushPendingDeathEffects();

    internal bool HasAliveUnits(BattleTeam team)
    {
        for (int i = 0; i < _unitOrder.Count; i++)
        {
            BattleUnit unit = _units[_unitOrder[i]];
            if (unit.IsAlive && unit.Team == team)
            {
                return true;
            }
        }

        return false;
    }

    internal bool TryGetPlayerHomePosition(int unitId, out FixVec2 position)
        => _playerHomePositions.TryGetValue(unitId, out position);

    internal void TickUnitBuffs(BattleUnit unit, Fix32 deltaTime)
        => _statusTickController.TickUnitBuffs(unit, deltaTime);

    internal void MarkUnitForRemoval(int unitId)
        => _unitLifecycleController.MarkUnitForRemoval(unitId);

    internal void EndBattle(BattleTeam winner)
    {
        _battleEnded = true;
        Winner = winner;
        _waveController.MarkBattleEnded();
        _events.Add(BattleEvent.BattleEnded(winner));
    }

    internal int AllocateUnitId()
        => _nextUnitId++;

    internal void RegisterRuntimeUnit(BattleUnit unit, FixVec2 homePosition)
    {
        _units.Add(unit.Id, unit);
        _unitOrder.Add(unit.Id);
        if (unit.Team == BattleTeam.TeamA)
        {
            _playerUnitIds.Add(unit.Id);
            _playerHomePositions[unit.Id] = homePosition;
        }
        else if (unit.Team == BattleTeam.TeamB)
        {
            _enemyUnitIds.Add(unit.Id);
        }
    }

    internal void RemoveUnit(int unitId)
    {
        if (!_units.TryGetValue(unitId, out BattleUnit unit))
        {
            return;
        }

        RemoveUnit(unitId, unit.Team);
    }

    private void RemoveUnit(int unitId, BattleTeam team)
    {
        if (_units.Remove(unitId))
        {
            _unitOrder.Remove(unitId);
            _playerUnitIds.Remove(unitId);
            _enemyUnitIds.Remove(unitId);
        }

        if (team == BattleTeam.TeamA)
        {
            _playerHomePositions.Remove(unitId);
        }
    }
}
