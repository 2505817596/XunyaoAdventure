using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly struct BattleSkillHit
{
    public readonly int TargetUnitId;
    public readonly BattleSkillSpec Skill;
    public readonly Fix32 EnergyGain;
    public readonly Fix32 ProjectileSpeed;
    public readonly BattleProjectileMotionType ProjectileMotionType;
    public readonly BattleProjectileImpactType ProjectileImpactType;
    public readonly BattleProjectileTargetMode ProjectileTargetMode;
    public readonly int ProjectileTargetCount;
    public readonly BattleSkillEffectSpec[] Effects;

    public BattleSkillHit(int targetUnitId, BattleSkillSpec skill)
        : this(targetUnitId, skill, new BattleSkillTimelineEvent(skill.WindupTime, skill.EnergyGainOnHit))
    {
    }

    public BattleSkillHit(int targetUnitId, BattleSkillSpec skill, BattleSkillTimelineEvent timelineEvent)
    {
        TargetUnitId = targetUnitId;
        Skill = skill;
        EnergyGain = timelineEvent.EnergyGain;
        ProjectileSpeed = timelineEvent.ProjectileSpeed;
        ProjectileMotionType = timelineEvent.ProjectileMotionType;
        ProjectileImpactType = timelineEvent.ProjectileImpactType;
        ProjectileTargetMode = timelineEvent.ProjectileTargetMode;
        ProjectileTargetCount = timelineEvent.ProjectileTargetCount;
        Effects = timelineEvent.Effects.Length > 0 ? timelineEvent.Effects : skill.Effects;
    }

    public BattleSkillHit WithTargetUnitId(int targetUnitId)
    {
        if (targetUnitId == TargetUnitId)
        {
            return this;
        }

        return new BattleSkillHit(
            targetUnitId,
            Skill,
            EnergyGain,
            ProjectileSpeed,
            ProjectileMotionType,
            ProjectileImpactType,
            ProjectileTargetMode,
            ProjectileTargetCount,
            Effects);
    }

    private BattleSkillHit(
        int targetUnitId,
        BattleSkillSpec skill,
        Fix32 energyGain,
        Fix32 projectileSpeed,
        BattleProjectileMotionType projectileMotionType,
        BattleProjectileImpactType projectileImpactType,
        BattleProjectileTargetMode projectileTargetMode,
        int projectileTargetCount,
        BattleSkillEffectSpec[] effects)
    {
        TargetUnitId = targetUnitId;
        Skill = skill;
        EnergyGain = energyGain;
        ProjectileSpeed = projectileSpeed;
        ProjectileMotionType = projectileMotionType;
        ProjectileImpactType = projectileImpactType;
        ProjectileTargetMode = projectileTargetMode;
        ProjectileTargetCount = projectileTargetCount;
        Effects = effects ?? System.Array.Empty<BattleSkillEffectSpec>();
    }
}
