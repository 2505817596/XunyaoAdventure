using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleUnit
{
    private BattleSkillSpec CreateRuntimeSkillSpec(BattleSkillSpec skill)
    {
        if (skill.Slot != BattleSkillSlot.BasicAttack)
        {
            return skill;
        }

        Fix32 attackSpeed = AttackSpeed;
        Fix32 minAttackSpeed = Fix32.FromDouble(0.1);
        if (attackSpeed < minAttackSpeed)
        {
            attackSpeed = minAttackSpeed;
        }

        if (attackSpeed == Fix32.One)
        {
            return skill;
        }

        BattleSkillTimelineEvent[] timelineEvents = new BattleSkillTimelineEvent[skill.TimelineEvents.Length];
        for (int i = 0; i < timelineEvents.Length; i++)
        {
            BattleSkillTimelineEvent timelineEvent = skill.TimelineEvents[i];
            timelineEvents[i] = new BattleSkillTimelineEvent(
                timelineEvent.Time / attackSpeed,
                timelineEvent.EnergyGain,
                timelineEvent.ProjectileSpeed,
                timelineEvent.ProjectileMotionType,
                timelineEvent.ProjectileImpactType,
                timelineEvent.ProjectileTargetMode,
                timelineEvent.ProjectileTargetCount,
                timelineEvent.Effects);
        }

        return new BattleSkillSpec(
            skill.Slot,
            skill.Type,
            skill.Range,
            skill.EffectRadius,
            skill.Damage,
            skill.WindupTime / attackSpeed,
            skill.RecoverTime / attackSpeed,
            skill.EnergyCost,
            skill.Cooldown,
            skill.EnergyGainOnHit,
            skill.RequiresRangeCheck,
            skill.TargetTeam,
            skill.Effects,
            timelineEvents);
    }

    private static Fix32 ReduceCooldownValue(Fix32 value, Fix32 deltaTime)
    {
        if (value <= Fix32.Zero)
        {
            return Fix32.Zero;
        }

        value -= deltaTime;
        return value < Fix32.Zero ? Fix32.Zero : value;
    }
}

