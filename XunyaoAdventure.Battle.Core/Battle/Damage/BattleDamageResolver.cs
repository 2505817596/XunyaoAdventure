using System.Collections.Generic;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleDamageResolver
{
    private readonly BattleWorld _world;
    private readonly List<(int targetUnitId, int sourceUnitId, Fix32 amount)> _pendingReflections = new();
    private readonly List<(int unitId, Fix32 amount)> _pendingLifeSteal = new();
    private static readonly Fix32 MitigationBase = Fix32.FromInt(300);
    private static readonly Fix32 MitigationLevelScale = Fix32.FromInt(12);
    private static readonly Fix32 MinimumPositiveDamage = Fix32.One;

    public BattleDamageResolver(BattleWorld world)
    {
        _world = world;
    }

    public void ApplyDamage(BattleUnit source, BattleUnit target, Fix32 damage, BattleDamageType damageType, BattleSkillType skillType)
        => ApplyDamage(source, target, damage, damageType, skillType, BattleSkillTag.Damage);

    public void ApplyDamage(BattleUnit source, BattleUnit target, Fix32 damage, BattleDamageType damageType, BattleSkillType skillType, BattleSkillTag tags)
    {
        Fix32 modifiedDamage = ResolveDamageAfterMitigation(source, target, source.ModifyOutgoingDamage(target, damage), damageType);
        modifiedDamage = target.ModifyIncomingDamage(source, modifiedDamage, damageType);
        Fix32 actualDamage = target.ReceiveDamage(modifiedDamage, damageType);
        _world.DispatchCombatEvent(new BattleCombatEvent(
            BattleCombatEventType.DamageDealt,
            source.Id,
            target.Id,
            actualDamage,
            target.Hp,
            source.Team,
            target.Team,
            BattleBuffType.None,
            skillType));
        _world.DispatchCombatEvent(new BattleCombatEvent(
            BattleCombatEventType.DamageTaken,
            source.Id,
            target.Id,
            actualDamage,
            target.Hp,
            source.Team,
            target.Team));

        bool canLifeSteal = !BattleSkillTagUtility.Has(tags, BattleSkillTag.NoLifeSteal);
        bool canReflect = !BattleSkillTagUtility.Has(tags, BattleSkillTag.NoReflection);
        Fix32 lifeSteal = canLifeSteal ? source.NotifyAfterDealDamage(target, actualDamage) : Fix32.Zero;
        if (target.IsAlive && canReflect)
        {
            Fix32 reflection = target.NotifyAfterReceiveDamage(source, actualDamage);
            if (reflection > Fix32.Zero)
            {
                _pendingReflections.Add((source.Id, target.Id, reflection));
            }
        }

        if (lifeSteal > Fix32.Zero)
        {
            _pendingLifeSteal.Add((source.Id, lifeSteal));
        }

        if (!target.IsAlive)
        {
            _world.TryTriggerKillPassives(source, target);
            _world.HandleUnitDied(target, source);
        }
        else if (target.WasRevivedThisHit)
        {
            _world.AddBattleEvent(BattleEvent.UnitDamaged(target.Id, source.Id, actualDamage, Fix32.Zero, target.Team));
            _world.AddBattleEvent(BattleEvent.UnitRevived(target.Id, source.Id, target.RevivedHpThisHit, target.Team));
            _world.AddBattleEvent(BattleEvent.BuffTriggered(target.Id, source.Id, BattleBuffType.Revive, target.RevivedHpThisHit, target.Team));
            return;
        }

        _world.TryTriggerDamageDealtPassives(source, target);
        _world.TryTriggerDamageTakenPassives(target, source);
        _world.TryTriggerPassiveEffects(target);
        _world.AddBattleEvent(BattleEvent.UnitDamaged(target.Id, source.Id, actualDamage, target.Hp, target.Team));
    }

    public void ApplyPeriodicDamage(BattleUnit source, BattleUnit target, Fix32 damage, BattleBuffType buffType)
    {
        if (!source.IsAlive || !target.IsAlive || damage <= Fix32.Zero)
        {
            return;
        }

        BattleDamageType damageType = BattleDamageType.Magical;
        Fix32 modifiedDamage = ResolveDamageAfterMitigation(source, target, damage, damageType);
        modifiedDamage = target.ModifyIncomingDamage(source, modifiedDamage, damageType);
        Fix32 actualDamage = target.ReceiveDamageFromBuff(modifiedDamage, damageType);
        if (actualDamage <= Fix32.Zero)
        {
            return;
        }

        _world.DispatchCombatEvent(new BattleCombatEvent(
            BattleCombatEventType.DamageTaken,
            source.Id,
            target.Id,
            actualDamage,
            target.Hp,
            source.Team,
            target.Team,
            buffType));

        _world.AddBattleEvent(BattleEvent.BuffTriggered(target.Id, source.Id, buffType, actualDamage, target.Team));
        _world.TryTriggerDamageDealtPassives(source, target);
        _world.TryTriggerDamageTakenPassives(target, source);
        _world.TryTriggerPassiveEffects(target);
        if (!target.IsAlive)
        {
            _world.TryTriggerKillPassives(source, target);
            _world.HandleUnitDied(target, source);
        }
        else if (target.WasRevivedThisHit)
        {
            _world.AddBattleEvent(BattleEvent.UnitDamaged(target.Id, source.Id, actualDamage, Fix32.Zero, target.Team));
            _world.AddBattleEvent(BattleEvent.UnitRevived(target.Id, source.Id, target.RevivedHpThisHit, target.Team));
            _world.AddBattleEvent(BattleEvent.BuffTriggered(target.Id, source.Id, BattleBuffType.Revive, target.RevivedHpThisHit, target.Team));
            return;
        }
        _world.AddBattleEvent(BattleEvent.UnitDamaged(target.Id, source.Id, actualDamage, target.Hp, target.Team));
    }

    public void FlushPendingTriggeredEffects()
    {
        for (int i = 0; i < _pendingReflections.Count; i++)
        {
            var entry = _pendingReflections[i];
            if (_world.Units.TryGetValue(entry.targetUnitId, out BattleUnit source) && source.IsAlive
                && _world.Units.TryGetValue(entry.sourceUnitId, out BattleUnit target) && target.IsAlive)
            {
                BattleDamageType damageType = BattleDamageType.Magical;
                Fix32 modifiedDamage = ResolveDamageAfterMitigation(target, source, entry.amount, damageType);
                modifiedDamage = source.ModifyIncomingDamage(target, modifiedDamage, damageType);
                Fix32 reflected = source.ReceiveDamageFromBuff(modifiedDamage, damageType);
                _world.AddBattleEvent(BattleEvent.BuffTriggered(source.Id, target.Id, BattleBuffType.Thorns, reflected, source.Team));
                if (!source.IsAlive)
                {
                    _world.HandleUnitDied(source, target);
                }
            }
        }

        _pendingReflections.Clear();

        for (int i = 0; i < _pendingLifeSteal.Count; i++)
        {
            var entry = _pendingLifeSteal[i];
                if (_world.Units.TryGetValue(entry.unitId, out BattleUnit unit) && unit.IsAlive)
                {
                    Fix32 before = unit.Hp;
                    unit.ReceiveHeal(entry.amount, unit);
                    Fix32 actual = unit.Hp - before;
                if (actual > Fix32.Zero)
                {
                    _world.DispatchCombatEvent(new BattleCombatEvent(
                        BattleCombatEventType.Healed,
                        unit.Id,
                        unit.Id,
                        actual,
                        unit.Hp,
                        unit.Team,
                        unit.Team));
                    _world.AddBattleEvent(BattleEvent.UnitHealed(unit.Id, unit.Id, actual, unit.Hp, unit.Team));
                    _world.AddBattleEvent(BattleEvent.BuffTriggered(unit.Id, unit.Id, BattleBuffType.LifeSteal, actual, unit.Team));
                }
            }
        }

        _pendingLifeSteal.Clear();
        _world.FlushPendingDeathEffects();
    }

    private static Fix32 ResolveDamageAfterMitigation(BattleUnit source, BattleUnit target, Fix32 damage, BattleDamageType damageType)
    {
        if (damage <= Fix32.Zero || damageType == BattleDamageType.Pure)
        {
            return damage > Fix32.Zero ? damage : Fix32.Zero;
        }

        Fix32 mitigationAttribute = target.ResolveDamageReductionAttribute(damageType);
        if (mitigationAttribute <= Fix32.Zero)
        {
            return damage;
        }

        Fix32 mitigationK = MitigationBase + MitigationLevelScale * Fix32.FromInt(source.Spec.Level);
        Fix32 result = ResolveMitigatedDamage(damage, mitigationK, mitigationAttribute);
        return result > Fix32.Zero && result < MinimumPositiveDamage ? MinimumPositiveDamage : result;
    }

    private static Fix32 ResolveMitigatedDamage(Fix32 damage, Fix32 mitigationK, Fix32 mitigationAttribute)
    {
        Fix32 denominator = mitigationK + mitigationAttribute;
        if (denominator <= Fix32.Zero)
        {
            return damage;
        }

        long raw = (long)damage.Raw * mitigationK.Raw / denominator.Raw;
        if (raw > int.MaxValue)
        {
            return Fix32.FromRaw(int.MaxValue);
        }

        if (raw <= 0)
        {
            return Fix32.Zero;
        }

        return Fix32.FromRaw((int)raw);
    }
}
