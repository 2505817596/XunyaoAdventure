using System.Collections.Generic;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleUnitLifecycleController
{
    private readonly BattleWorld _world;
    private readonly List<int> _deadUnits = new();
    private readonly List<PendingSummon> _pendingSummons = new();
    private readonly List<PendingDeathEffect> _pendingDeathEffects = new();

    public BattleUnitLifecycleController(BattleWorld world)
    {
        _world = world;
    }

    public void HandleUnitDied(BattleUnit deadUnit, BattleUnit source)
    {
        _world.AddBattleEvent(BattleEvent.UnitDied(deadUnit.Id, source.Id, deadUnit.Team));
        _world.DispatchCombatEvent(new BattleCombatEvent(
            BattleCombatEventType.UnitDied,
            source.Id,
            deadUnit.Id,
            Fix32.One,
            Fix32.Zero,
            source.Team,
            deadUnit.Team));
        _world.TriggerUnitDeathPassives(deadUnit);
        QueueDeathEffects(deadUnit);
        MarkUnitForRemoval(deadUnit.Id);
    }

    public void MarkUnitForRemoval(int unitId)
    {
        if (!_deadUnits.Contains(unitId))
        {
            _deadUnits.Add(unitId);
        }
    }

    public void QueueSummon(BattleUnit source, FixVec2 position, int skillProfileId, Fix32 hpScale, Fix32 duration)
    {
        if (skillProfileId < 0)
        {
            return;
        }

        Fix32 scale = hpScale;
        if (scale <= Fix32.Zero)
        {
            scale = Fix32.FromDouble(0.6);
        }

        Fix32 maxHp = source.Spec.MaxHp * scale;
        if (maxHp <= Fix32.Zero)
        {
            maxHp = Fix32.FromInt(1);
        }

        BattleUnitSpec spec = new(
            maxHp,
            source.Spec.MoveSpeed,
            source.Spec.AttackRange,
            source.Spec.AttackInterval,
            source.Spec.PhysicalAttack * scale,
            source.Spec.UltimateDamage * scale,
            source.Spec.UltimateRadius,
            source.Spec.UltimateEnergyCost,
            source.Spec.EnergyRegenPerSecond,
            source.Spec.UltimateCooldown,
            source.Spec.Radius,
            source.Spec.AggroRange,
            BattleControlMode.AutoCombat,
            BattleUnitTag.Summon,
            source.Spec.PrimaryAttribute);

        _pendingSummons.Add(new PendingSummon(source.Team, position, skillProfileId, spec, duration));
    }

    public void FlushPendingSummons()
    {
        if (_pendingSummons.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _pendingSummons.Count; i++)
        {
            PendingSummon summon = _pendingSummons[i];
            int id = _world.AllocateUnitId();
            BattleUnit unit = new(id, summon.Team, summon.Spec, summon.SkillProfileId, summon.Position);
            unit.SetFacing(summon.Team == BattleTeam.TeamB ? FixVec2.Left : FixVec2.Right);
            if (summon.Duration > Fix32.Zero)
            {
                unit.ApplyBuff(new SummonLifeBuff(summon.Duration));
            }

            _world.RegisterRuntimeUnit(unit, summon.Position);
            _world.AddBattleEvent(BattleEvent.UnitSpawned(id, summon.Position, unit.Hp, summon.Team, summon.SkillProfileId));
        }

        _pendingSummons.Clear();
    }

    public void FlushPendingDeathEffects()
    {
        if (_pendingDeathEffects.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _pendingDeathEffects.Count; i++)
        {
            PendingDeathEffect entry = _pendingDeathEffects[i];
            if (entry.Effect.Type == BattleSkillEffectType.Summon)
            {
                QueueSummon(entry.Source, entry.Position, entry.Effect.Value, entry.Effect.Amount, entry.Effect.StatusDuration);
            }
        }

        _pendingDeathEffects.Clear();
    }

    public void CleanupDeadUnits()
    {
        if (_deadUnits.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _deadUnits.Count; i++)
        {
            _world.RemoveUnit(_deadUnits[i]);
        }

        _deadUnits.Clear();
    }

    private void QueueDeathEffects(BattleUnit deadUnit)
    {
        if (deadUnit.SkillProfile.DeathEffects.Length == 0)
        {
            return;
        }

        for (int i = 0; i < deadUnit.SkillProfile.DeathEffects.Length; i++)
        {
            BattleSkillEffectSpec effect = deadUnit.SkillProfile.DeathEffects[i].Build(deadUnit.Spec);
            _pendingDeathEffects.Add(new PendingDeathEffect(deadUnit, effect));
        }
    }

    private readonly struct PendingSummon
    {
        public readonly BattleTeam Team;
        public readonly FixVec2 Position;
        public readonly int SkillProfileId;
        public readonly BattleUnitSpec Spec;
        public readonly Fix32 Duration;

        public PendingSummon(BattleTeam team, FixVec2 position, int skillProfileId, BattleUnitSpec spec, Fix32 duration)
        {
            Team = team;
            Position = position;
            SkillProfileId = skillProfileId;
            Spec = spec;
            Duration = duration;
        }
    }

    private readonly struct PendingDeathEffect
    {
        public readonly BattleUnit Source;
        public readonly FixVec2 Position;
        public readonly BattleSkillEffectSpec Effect;

        public PendingDeathEffect(BattleUnit source, BattleSkillEffectSpec effect)
        {
            Source = source;
            Position = source.Position;
            Effect = effect;
        }
    }
}
