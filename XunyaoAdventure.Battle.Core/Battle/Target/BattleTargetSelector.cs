using System.Collections.Generic;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleTargetSelector
{
    private readonly BattleWorld _world;

    public BattleTargetSelector(BattleWorld world)
    {
        _world = world;
    }

    public void ForEachSelectedUnit(BattleUnit source, BattleUnit primaryTarget, BattleSkillEffectSpec effect, System.Action<BattleUnit> action)
    {
        System.Action<BattleUnit> conditionalAction = unit =>
        {
            if (effect.Condition.Matches(source, unit))
            {
                action(unit);
            }
        };

        switch (effect.Target)
        {
            case BattleSkillEffectTarget.PrimaryTarget:
                conditionalAction(primaryTarget);
                break;
            case BattleSkillEffectTarget.Self:
                conditionalAction(source);
                break;
            case BattleSkillEffectTarget.EnemyUnitsInRadius:
                ForEachUnitInRadius(source, primaryTarget.Position, effect.Radius, BattleSkillTargetTeam.Enemy, conditionalAction);
                break;
            case BattleSkillEffectTarget.AllyUnitsInRadius:
                ForEachUnitInRadius(source, primaryTarget.Position, effect.Radius, BattleSkillTargetTeam.Ally, conditionalAction);
                break;
            case BattleSkillEffectTarget.AllEnemyUnits:
                ForEachUnitInTeam(source, BattleSkillTargetTeam.Enemy, conditionalAction);
                break;
            case BattleSkillEffectTarget.AllAllyUnits:
                ForEachUnitInTeam(source, BattleSkillTargetTeam.Ally, conditionalAction);
                break;
            case BattleSkillEffectTarget.LowestHpEnemy:
                SelectOneUnit(source, BattleSkillTargetTeam.Enemy, effect.Condition, LowestHpComparer, action, false);
                break;
            case BattleSkillEffectTarget.LowestHpAlly:
                SelectOneUnit(source, BattleSkillTargetTeam.Ally, effect.Condition, LowestHpComparer, action, false);
                break;
            case BattleSkillEffectTarget.RandomEnemy:
                SelectOneUnit(source, BattleSkillTargetTeam.Enemy, effect.Condition, LowestHpComparer, action, true);
                break;
            case BattleSkillEffectTarget.RandomAlly:
                SelectOneUnit(source, BattleSkillTargetTeam.Ally, effect.Condition, LowestHpComparer, action, true);
                break;
            case BattleSkillEffectTarget.NearestEnemy:
                SelectOneUnit(source, BattleSkillTargetTeam.Enemy, effect.Condition, NearestComparer(source), action, false);
                break;
            case BattleSkillEffectTarget.NearestAlly:
                SelectOneUnit(source, BattleSkillTargetTeam.Ally, effect.Condition, NearestComparer(source), action, false);
                break;
            case BattleSkillEffectTarget.FrontEnemy:
                SelectFrontUnit(source, BattleSkillTargetTeam.Enemy, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.FrontAlly:
                SelectFrontUnit(source, BattleSkillTargetTeam.Ally, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.EnemyUnitsInLine:
                ForEachUnitInLine(source, primaryTarget, effect.Radius, BattleSkillTargetTeam.Enemy, conditionalAction);
                break;
            case BattleSkillEffectTarget.AllyUnitsInLine:
                ForEachUnitInLine(source, primaryTarget, effect.Radius, BattleSkillTargetTeam.Ally, conditionalAction);
                break;
            case BattleSkillEffectTarget.EnemyUnitsInFan:
                ForEachUnitInFan(source, primaryTarget, effect.Radius, BattleSkillTargetTeam.Enemy, conditionalAction);
                break;
            case BattleSkillEffectTarget.AllyUnitsInFan:
                ForEachUnitInFan(source, primaryTarget, effect.Radius, BattleSkillTargetTeam.Ally, conditionalAction);
                break;
            case BattleSkillEffectTarget.ChainedEnemies:
                ForEachChainedUnit(source, primaryTarget, effect.Radius, effect.Value, BattleSkillTargetTeam.Enemy, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.ChainedAllies:
                ForEachChainedUnit(source, primaryTarget, effect.Radius, effect.Value, BattleSkillTargetTeam.Ally, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.BackEnemy:
                SelectRowUnit(source, BattleSkillTargetTeam.Enemy, BattleFormationRow.Back, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.BackAlly:
                SelectRowUnit(source, BattleSkillTargetTeam.Ally, BattleFormationRow.Back, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.MiddleEnemy:
                SelectRowUnit(source, BattleSkillTargetTeam.Enemy, BattleFormationRow.Middle, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.MiddleAlly:
                SelectRowUnit(source, BattleSkillTargetTeam.Ally, BattleFormationRow.Middle, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.FrontEnemyRow:
                ForEachUnitInFormationRow(source, BattleSkillTargetTeam.Enemy, BattleFormationRow.Front, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.FrontAllyRow:
                ForEachUnitInFormationRow(source, BattleSkillTargetTeam.Ally, BattleFormationRow.Front, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.BackEnemyRow:
                ForEachUnitInFormationRow(source, BattleSkillTargetTeam.Enemy, BattleFormationRow.Back, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.BackAllyRow:
                ForEachUnitInFormationRow(source, BattleSkillTargetTeam.Ally, BattleFormationRow.Back, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.MiddleEnemyRow:
                ForEachUnitInFormationRow(source, BattleSkillTargetTeam.Enemy, BattleFormationRow.Middle, effect.Condition, action);
                break;
            case BattleSkillEffectTarget.MiddleAllyRow:
                ForEachUnitInFormationRow(source, BattleSkillTargetTeam.Ally, BattleFormationRow.Middle, effect.Condition, action);
                break;
        }
    }

    public void ForEachSelectedUnitAtPosition(BattleUnit source, FixVec2 center, BattleSkillEffectSpec effect, System.Action<BattleUnit> action)
    {
        System.Action<BattleUnit> conditionalAction = unit =>
        {
            if (effect.Condition.Matches(source, unit))
            {
                action(unit);
            }
        };

        switch (effect.Target)
        {
            case BattleSkillEffectTarget.EnemyUnitsInRadius:
                ForEachUnitInRadius(source, center, effect.Radius, BattleSkillTargetTeam.Enemy, conditionalAction);
                break;
            case BattleSkillEffectTarget.AllyUnitsInRadius:
                ForEachUnitInRadius(source, center, effect.Radius, BattleSkillTargetTeam.Ally, conditionalAction);
                break;
        }
    }

    public void ForEachUnitInRadius(BattleUnit source, FixVec2 center, Fix32 radius, BattleSkillTargetTeam targetTeam, System.Action<BattleUnit> action)
    {
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive || !MatchesTargetTeam(source, other, targetTeam))
            {
                continue;
            }

            if (FixVec2.Distance(other.Position, center) <= radius)
            {
                action(other);
            }
        }
    }

    public void CollectProjectileTargets(BattleUnit source, BattleUnit primaryTarget, BattleSkillHit hit, List<BattleUnit> targets)
    {
        targets.Clear();
        int maxTargets = hit.ProjectileTargetCount <= 0 ? 1 : hit.ProjectileTargetCount;
        switch (hit.ProjectileTargetMode)
        {
            case BattleProjectileTargetMode.NearestEnemies:
                SelectProjectileTargets(source, BattleSkillTargetTeam.Enemy, default, maxTargets, NearestComparer(source), false, targets);
                break;
            case BattleProjectileTargetMode.RandomEnemies:
                SelectProjectileTargets(source, BattleSkillTargetTeam.Enemy, default, maxTargets, NearestComparer(source), true, targets);
                break;
            case BattleProjectileTargetMode.LowestHpEnemies:
                SelectProjectileTargets(source, BattleSkillTargetTeam.Enemy, default, maxTargets, LowestHpComparer, false, targets);
                break;
            case BattleProjectileTargetMode.FrontEnemyRow:
                CollectProjectileFormationRowTargets(source, BattleSkillTargetTeam.Enemy, default, BattleFormationRow.Front, targets);
                TrimTargets(targets, maxTargets);
                break;
            case BattleProjectileTargetMode.BackEnemyRow:
                CollectProjectileFormationRowTargets(source, BattleSkillTargetTeam.Enemy, default, BattleFormationRow.Back, targets);
                TrimTargets(targets, maxTargets);
                break;
            case BattleProjectileTargetMode.NearestAllies:
                SelectProjectileTargets(source, BattleSkillTargetTeam.Ally, default, maxTargets, NearestComparer(source), false, targets);
                break;
            case BattleProjectileTargetMode.RandomAllies:
                SelectProjectileTargets(source, BattleSkillTargetTeam.Ally, default, maxTargets, NearestComparer(source), true, targets);
                break;
            case BattleProjectileTargetMode.LowestHpAllies:
                SelectProjectileTargets(source, BattleSkillTargetTeam.Ally, default, maxTargets, LowestHpComparer, false, targets);
                break;
            default:
                if (primaryTarget.IsAlive)
                {
                    targets.Add(primaryTarget);
                }
                break;
        }
    }

    private void SelectProjectileTargets(
        BattleUnit source,
        BattleSkillTargetTeam targetTeam,
        BattleSkillEffectCondition condition,
        int maxTargets,
        System.Comparison<BattleUnit> comparer,
        bool randomPick,
        List<BattleUnit> targets)
    {
        List<BattleUnit> candidates = new();
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (other.IsAlive && MatchesTargetTeam(source, other, targetTeam) && condition.Matches(source, other))
            {
                candidates.Add(other);
            }
        }

        for (int i = 0; i < maxTargets && candidates.Count > 0; i++)
        {
            int selectedIndex;
            if (randomPick)
            {
                selectedIndex = _world.NextRandomInt(candidates.Count);
            }
            else
            {
                selectedIndex = 0;
                for (int j = 1; j < candidates.Count; j++)
                {
                    if (comparer(candidates[j], candidates[selectedIndex]) < 0)
                    {
                        selectedIndex = j;
                    }
                }
            }

            targets.Add(candidates[selectedIndex]);
            candidates.RemoveAt(selectedIndex);
        }
    }

    private void CollectProjectileFormationRowTargets(BattleUnit source, BattleSkillTargetTeam targetTeam, BattleSkillEffectCondition condition, BattleFormationRow row, List<BattleUnit> targets)
    {
        targets.Clear();
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (other.IsAlive && MatchesTargetTeam(source, other, targetTeam) && condition.Matches(source, other))
            {
                targets.Add(other);
            }
        }

        FilterTargetsByPrimaryAttribute(targets, ResolvePrimaryAttribute(row));
        if (targets.Count > 0)
        {
            SortByFormationDepth(source, targets);
            return;
        }

        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (other.IsAlive && MatchesTargetTeam(source, other, targetTeam) && condition.Matches(source, other))
            {
                targets.Add(other);
            }
        }

        if (targets.Count <= 1)
        {
            return;
        }

        SortByFormationDepth(source, targets);

        int third = (targets.Count + 2) / 3;
        int start = row == BattleFormationRow.Back ? targets.Count - third : 0;
        int end = row == BattleFormationRow.Back ? targets.Count : third;
        if (start <= 0 && end >= targets.Count)
        {
            return;
        }

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (i < start || i >= end)
            {
                targets.RemoveAt(i);
            }
        }
    }

    private static void TrimTargets(List<BattleUnit> targets, int maxTargets)
    {
        for (int i = targets.Count - 1; i >= maxTargets; i--)
        {
            targets.RemoveAt(i);
        }
    }

    private void SelectRowUnit(BattleUnit source, BattleSkillTargetTeam targetTeam, BattleFormationRow row, BattleSkillEffectCondition condition, System.Action<BattleUnit> action)
    {
        List<BattleUnit> candidates = BuildFormationRowCandidates(source, targetTeam, row, condition);
        if (candidates.Count == 0)
        {
            return;
        }

        action(candidates[0]);
    }

    private void ForEachUnitInFormationRow(BattleUnit source, BattleSkillTargetTeam targetTeam, BattleFormationRow row, BattleSkillEffectCondition condition, System.Action<BattleUnit> action)
    {
        List<BattleUnit> candidates = BuildFormationRowCandidates(source, targetTeam, row, condition);
        for (int i = 0; i < candidates.Count; i++)
        {
            action(candidates[i]);
        }
    }

    private List<BattleUnit> BuildFormationRowCandidates(BattleUnit source, BattleSkillTargetTeam targetTeam, BattleFormationRow row, BattleSkillEffectCondition condition)
    {
        List<BattleUnit> candidates = new();
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive || !MatchesTargetTeam(source, other, targetTeam) || !condition.Matches(source, other))
            {
                continue;
            }

            candidates.Add(other);
        }

        FilterTargetsByPrimaryAttribute(candidates, ResolvePrimaryAttribute(row));
        if (candidates.Count > 0)
        {
            SortByFormationDepth(source, candidates);
            return candidates;
        }

        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive || !MatchesTargetTeam(source, other, targetTeam) || !condition.Matches(source, other))
            {
                continue;
            }

            candidates.Add(other);
        }

        if (candidates.Count <= 1)
        {
            return candidates;
        }

        SortByFormationDepth(source, candidates);

        int start;
        int end;
        int third = (candidates.Count + 2) / 3;
        switch (row)
        {
            case BattleFormationRow.Front:
                start = 0;
                end = third;
                break;
            case BattleFormationRow.Middle:
                start = third;
                end = candidates.Count - third;
                if (start >= end)
                {
                    start = candidates.Count / 2;
                    end = start + 1;
                }
                break;
            default:
                start = candidates.Count - third;
                end = candidates.Count;
                break;
        }

        List<BattleUnit> rowUnits = new();
        for (int i = start; i < end; i++)
        {
            rowUnits.Add(candidates[i]);
        }

        return rowUnits;
    }

    private static Fix32 GetFormationDepth(BattleUnit source, BattleUnit target)
        => FixVec2.Dot(target.Position - source.Position, ResolveBattleDirection(source));

    private static void FilterTargetsByPrimaryAttribute(List<BattleUnit> targets, BattlePrimaryAttribute primaryAttribute)
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i].Spec.PrimaryAttribute != primaryAttribute)
            {
                targets.RemoveAt(i);
            }
        }
    }

    private static BattlePrimaryAttribute ResolvePrimaryAttribute(BattleFormationRow row)
    {
        return row switch
        {
            BattleFormationRow.Middle => BattlePrimaryAttribute.Intelligence,
            BattleFormationRow.Back => BattlePrimaryAttribute.Agility,
            _ => BattlePrimaryAttribute.Strength,
        };
    }

    private static void SortByFormationDepth(BattleUnit source, List<BattleUnit> targets)
    {
        targets.Sort((left, right) =>
        {
            int depthCompare = GetFormationDepth(source, left).Raw.CompareTo(GetFormationDepth(source, right).Raw);
            return depthCompare != 0 ? depthCompare : left.Id.CompareTo(right.Id);
        });
    }

    private void ForEachUnitInLine(BattleUnit source, BattleUnit primaryTarget, Fix32 width, BattleSkillTargetTeam targetTeam, System.Action<BattleUnit> action)
    {
        FixVec2 direction = ResolveDirection(source, primaryTarget);
        Fix32 maxForward = FixVec2.Distance(source.Position, primaryTarget.Position) + width;
        Fix32 halfWidth = width * Fix32.Half;
        if (halfWidth <= FixMath.Epsilon)
        {
            halfWidth = source.Spec.Radius + primaryTarget.Spec.Radius;
        }

        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive || !MatchesTargetTeam(source, other, targetTeam))
            {
                continue;
            }

            FixVec2 offset = other.Position - source.Position;
            Fix32 forward = FixVec2.Dot(offset, direction);
            if (forward < Fix32.Zero || forward > maxForward)
            {
                continue;
            }

            Fix32 lateral = FixMath.Abs(FixVec2.Cross(offset, direction));
            if (lateral <= halfWidth + other.Spec.Radius)
            {
                action(other);
            }
        }
    }

    private void ForEachUnitInFan(BattleUnit source, BattleUnit primaryTarget, Fix32 distance, BattleSkillTargetTeam targetTeam, System.Action<BattleUnit> action)
    {
        FixVec2 direction = ResolveDirection(source, primaryTarget);
        Fix32 cosHalfAngle = Fix32.Half;
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive || !MatchesTargetTeam(source, other, targetTeam))
            {
                continue;
            }

            FixVec2 offset = other.Position - source.Position;
            Fix32 range = offset.Magnitude;
            if (range <= FixMath.Epsilon || range > distance + other.Spec.Radius)
            {
                continue;
            }

            FixVec2 otherDirection = offset / range;
            if (FixVec2.Dot(otherDirection, direction) >= cosHalfAngle)
            {
                action(other);
            }
        }
    }

    private void ForEachChainedUnit(BattleUnit source, BattleUnit primaryTarget, Fix32 jumpRadius, int maxTargets, BattleSkillTargetTeam targetTeam, BattleSkillEffectCondition condition, System.Action<BattleUnit> action)
    {
        if (maxTargets <= 0)
        {
            maxTargets = 1;
        }

        List<int> selectedIds = new();
        BattleUnit? current = null;
        if (primaryTarget.IsAlive && MatchesTargetTeam(source, primaryTarget, targetTeam) && condition.Matches(source, primaryTarget))
        {
            current = primaryTarget;
        }
        else
        {
            current = FindNearestUnselected(source, source.Position, jumpRadius, targetTeam, condition, selectedIds);
        }

        for (int i = 0; i < maxTargets && current != null; i++)
        {
            selectedIds.Add(current.Id);
            action(current);
            current = FindNearestUnselected(source, current.Position, jumpRadius, targetTeam, condition, selectedIds);
        }
    }

    private BattleUnit? FindNearestUnselected(BattleUnit source, FixVec2 center, Fix32 radius, BattleSkillTargetTeam targetTeam, BattleSkillEffectCondition condition, List<int> selectedIds)
    {
        BattleUnit? best = null;
        Fix32 bestDistance = Fix32.Zero;
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive || selectedIds.Contains(other.Id) || !MatchesTargetTeam(source, other, targetTeam) || !condition.Matches(source, other))
            {
                continue;
            }

            Fix32 distance = FixVec2.Distance(other.Position, center);
            if (radius > Fix32.Zero && distance > radius)
            {
                continue;
            }

            if (best == null || distance < bestDistance || (distance == bestDistance && other.Id < best.Id))
            {
                best = other;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static FixVec2 ResolveDirection(BattleUnit source, BattleUnit primaryTarget)
    {
        FixVec2 delta = primaryTarget.Position - source.Position;
        if (delta.SqrMagnitude > FixMath.Epsilon)
        {
            return FixVec2.Normalize(delta);
        }

        return ResolveBattleDirection(source);
    }

    private void ForEachUnitInTeam(BattleUnit source, BattleSkillTargetTeam targetTeam, System.Action<BattleUnit> action)
    {
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive || !MatchesTargetTeam(source, other, targetTeam))
            {
                continue;
            }

            action(other);
        }
    }

    private void SelectOneUnit(BattleUnit source, BattleSkillTargetTeam targetTeam, BattleSkillEffectCondition condition, System.Comparison<BattleUnit> comparer, System.Action<BattleUnit> action, bool randomPick)
    {
        List<BattleUnit> candidates = new();
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive || !MatchesTargetTeam(source, other, targetTeam) || !condition.Matches(source, other))
            {
                continue;
            }

            candidates.Add(other);
        }

        if (candidates.Count == 0)
        {
            return;
        }

        if (randomPick)
        {
            action(candidates[_world.NextRandomInt(candidates.Count)]);
            return;
        }

        BattleUnit best = candidates[0];
        for (int i = 1; i < candidates.Count; i++)
        {
            if (comparer(candidates[i], best) < 0)
            {
                best = candidates[i];
            }
        }

        action(best);
    }

    private void SelectFrontUnit(BattleUnit source, BattleSkillTargetTeam targetTeam, BattleSkillEffectCondition condition, System.Action<BattleUnit> action)
    {
        IReadOnlyList<int> unitOrder = _world.UnitOrder;
        BattleUnit? best = null;
        Fix32 bestForward = Fix32.Zero;
        FixVec2 direction = ResolveBattleDirection(source);
        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive
                || other.Spec.PrimaryAttribute != BattlePrimaryAttribute.Strength
                || !MatchesTargetTeam(source, other, targetTeam)
                || !condition.Matches(source, other))
            {
                continue;
            }

            FixVec2 offset = other.Position - source.Position;
            Fix32 forward = FixVec2.Dot(offset, direction);
            if (forward <= Fix32.Zero)
            {
                continue;
            }

            if (best == null || forward < bestForward)
            {
                best = other;
                bestForward = forward;
            }
        }

        if (best != null)
        {
            action(best);
            return;
        }

        for (int i = 0; i < unitOrder.Count; i++)
        {
            BattleUnit other = _world.Units[unitOrder[i]];
            if (!other.IsAlive || !MatchesTargetTeam(source, other, targetTeam) || !condition.Matches(source, other))
            {
                continue;
            }

            FixVec2 offset = other.Position - source.Position;
            Fix32 forward = FixVec2.Dot(offset, direction);
            if (forward <= Fix32.Zero)
            {
                continue;
            }

            if (best == null || forward < bestForward)
            {
                best = other;
                bestForward = forward;
            }
        }

        if (best != null)
        {
            action(best);
        }
    }

    private static FixVec2 ResolveBattleDirection(BattleUnit source)
        => source.Team == BattleTeam.TeamB ? FixVec2.Left : FixVec2.Right;

    private System.Comparison<BattleUnit> NearestComparer(BattleUnit source)
    {
        return (left, right) =>
        {
            int distanceCompare = FixVec2.Distance(left.Position, source.Position).Raw.CompareTo(FixVec2.Distance(right.Position, source.Position).Raw);
            if (distanceCompare != 0)
            {
                return distanceCompare;
            }

            return left.Id.CompareTo(right.Id);
        };
    }

    private static int LowestHpComparer(BattleUnit left, BattleUnit right)
    {
        int hpCompare = left.Hp.Raw.CompareTo(right.Hp.Raw);
        if (hpCompare != 0)
        {
            return hpCompare;
        }

        return left.Id.CompareTo(right.Id);
    }

    private static bool MatchesTargetTeam(BattleUnit source, BattleUnit target, BattleSkillTargetTeam targetTeam)
    {
        return targetTeam switch
        {
            BattleSkillTargetTeam.Ally => target.Team == source.Team,
            BattleSkillTargetTeam.Self => target.Id == source.Id,
            _ => target.Team != source.Team,
        };
    }

    private enum BattleFormationRow
    {
        Front = 0,
        Middle = 1,
        Back = 2,
    }
}
