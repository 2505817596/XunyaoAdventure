using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleWorld
{
    private BattleUnit? ResolveTauntTarget(BattleUnit unit)
    {
        return ResolveEnemyTarget(unit, requireTaunt: true);
    }

    internal BattleUnit? ResolveTarget(BattleUnit unit)
    {
        BattleUnit? tauntTarget = ResolveTauntTarget(unit);
        if (tauntTarget != null)
        {
            unit.SetTarget(tauntTarget.Id);
            return tauntTarget;
        }

        BattleUnit? best = ResolveEnemyTarget(unit, requireTaunt: false);
        if (best != null)
        {
            unit.SetTarget(best.Id);
        }

        return best;
    }

    private BattleUnit? ResolveSkillTarget(BattleUnit unit, int targetUnitId)
    {
        BattleSkillSpec ultimate = unit.Skills.GetSkill(BattleSkillSlot.Ultimate);
        return ResolveSkillTarget(unit, ultimate, targetUnitId);
    }

    internal BattleUnit? ResolveSkillTarget(BattleUnit unit, BattleSkillSpec skill, int targetUnitId)
    {
        if (targetUnitId != 0 && _units.TryGetValue(targetUnitId, out BattleUnit explicitTarget) && explicitTarget.IsAlive)
        {
            if (skill.TargetTeam == BattleSkillTargetTeam.Self && explicitTarget.Id == unit.Id)
            {
                return explicitTarget;
            }

            if (skill.TargetTeam == BattleSkillTargetTeam.Ally && explicitTarget.Team == unit.Team)
            {
                return explicitTarget;
            }

            if (skill.TargetTeam == BattleSkillTargetTeam.Enemy && explicitTarget.Team != unit.Team)
            {
                return explicitTarget;
            }
        }

        if (skill.TargetTeam == BattleSkillTargetTeam.Self)
        {
            return unit;
        }

        if (skill.TargetTeam == BattleSkillTargetTeam.Ally)
        {
            BattleUnit? best = null;
            Fix32 bestDistance = Fix32.Zero;
            bool found = false;

            for (int i = 0; i < _unitOrder.Count; i++)
            {
                BattleUnit other = _units[_unitOrder[i]];
                if (!other.IsAlive || other.Team != unit.Team)
                {
                    continue;
                }

                Fix32 distance = FixVec2.Distance(unit.Position, other.Position);
                if (distance > unit.Spec.AggroRange)
                {
                    continue;
                }

                if (!found || distance < bestDistance)
                {
                    found = true;
                    bestDistance = distance;
                    best = other;
                }
            }

            return best;
        }

        return ResolveTarget(unit);
    }

    private BattleUnit? ResolveEnemyTarget(BattleUnit unit, bool requireTaunt)
    {
        BattleUnit? bestForward = null;
        Fix32 bestForwardDistance = Fix32.Zero;
        Fix32 bestForwardRange = Fix32.Zero;
        bool foundForward = false;

        BattleUnit? bestAny = null;
        Fix32 bestAnyDistance = Fix32.Zero;
        bool foundAny = false;

        for (int i = 0; i < _unitOrder.Count; i++)
        {
            BattleUnit other = _units[_unitOrder[i]];
            if (!other.IsAlive || other.Team == unit.Team || (requireTaunt && !other.IsTaunted))
            {
                continue;
            }

            Fix32 distance = FixVec2.Distance(unit.Position, other.Position);
            if (distance > unit.Spec.AggroRange)
            {
                continue;
            }

            if (!foundAny || distance < bestAnyDistance || (distance == bestAnyDistance && other.Id < bestAny!.Id))
            {
                foundAny = true;
                bestAnyDistance = distance;
                bestAny = other;
            }

            Fix32 forward = GetBattleForwardDistance(unit, other);
            if (forward < Fix32.Zero)
            {
                continue;
            }

            if (!foundForward
                || forward < bestForwardDistance
                || (forward == bestForwardDistance && distance < bestForwardRange)
                || (forward == bestForwardDistance && distance == bestForwardRange && other.Id < bestForward!.Id))
            {
                foundForward = true;
                bestForwardDistance = forward;
                bestForwardRange = distance;
                bestForward = other;
            }
        }

        return bestForward ?? bestAny;
    }

    private static FixVec2 ResolveBattleForwardDirection(BattleUnit unit)
        => unit.Team == BattleTeam.TeamB ? FixVec2.Left : FixVec2.Right;

    private static Fix32 GetBattleForwardDistance(BattleUnit source, BattleUnit target)
        => FixVec2.Dot(target.Position - source.Position, ResolveBattleForwardDirection(source));

    internal void SpawnProjectiles(BattleUnit source, BattleUnit primaryTarget, BattleSkillHit hit)
    {
        _targetSelector.CollectProjectileTargets(source, primaryTarget, hit, _projectileTargets);
        for (int i = 0; i < _projectileTargets.Count; i++)
        {
            BattleUnit target = _projectileTargets[i];
            SpawnProjectile(source, target, hit.WithTargetUnitId(target.Id));
        }

        _projectileTargets.Clear();
    }
}
