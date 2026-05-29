using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly struct BattleSkillTimelineEvent
{
    public BattleSkillTimelineEvent(Fix32 time, Fix32 energyGain)
        : this(time, energyGain, Fix32.Zero, BattleProjectileMotionType.None, BattleProjectileImpactType.PrimaryTarget, BattleProjectileTargetMode.PrimaryTarget, 1, System.Array.Empty<BattleSkillEffectSpec>())
    {
    }

    public BattleSkillTimelineEvent(Fix32 time, Fix32 energyGain, BattleSkillEffectSpec[] effects)
        : this(time, energyGain, Fix32.Zero, BattleProjectileMotionType.None, BattleProjectileImpactType.PrimaryTarget, BattleProjectileTargetMode.PrimaryTarget, 1, effects)
    {
    }

    public BattleSkillTimelineEvent(Fix32 time, Fix32 energyGain, Fix32 projectileSpeed, BattleSkillEffectSpec[] effects)
        : this(time, energyGain, projectileSpeed, projectileSpeed > Fix32.Zero ? BattleProjectileMotionType.Homing : BattleProjectileMotionType.None, BattleProjectileImpactType.PrimaryTarget, BattleProjectileTargetMode.PrimaryTarget, 1, effects)
    {
    }

    public BattleSkillTimelineEvent(Fix32 time, Fix32 energyGain, Fix32 projectileSpeed, BattleProjectileMotionType projectileMotionType, BattleProjectileImpactType projectileImpactType, BattleSkillEffectSpec[] effects)
        : this(time, energyGain, projectileSpeed, projectileMotionType, projectileImpactType, BattleProjectileTargetMode.PrimaryTarget, 1, effects)
    {
    }

    public BattleSkillTimelineEvent(Fix32 time, Fix32 energyGain, Fix32 projectileSpeed, BattleProjectileMotionType projectileMotionType, BattleProjectileImpactType projectileImpactType, BattleProjectileTargetMode projectileTargetMode, int projectileTargetCount, BattleSkillEffectSpec[] effects)
    {
        Time = time;
        EnergyGain = energyGain;
        ProjectileSpeed = projectileSpeed;
        ProjectileMotionType = projectileMotionType;
        ProjectileImpactType = projectileImpactType;
        ProjectileTargetMode = projectileTargetMode;
        ProjectileTargetCount = projectileTargetCount;
        Effects = effects ?? System.Array.Empty<BattleSkillEffectSpec>();
    }

    public readonly Fix32 Time;
    public readonly Fix32 EnergyGain;
    public readonly Fix32 ProjectileSpeed;
    public readonly BattleProjectileMotionType ProjectileMotionType;
    public readonly BattleProjectileImpactType ProjectileImpactType;
    public readonly BattleProjectileTargetMode ProjectileTargetMode;
    public readonly int ProjectileTargetCount;
    public readonly BattleSkillEffectSpec[] Effects;
}

public readonly struct BattleSkillTimelineEventTemplate
{
    public BattleSkillTimelineEventTemplate(Fix32 time, Fix32 energyGain, BattleSkillEffectTemplate[] effects)
        : this(time, energyGain, Fix32.Zero, BattleProjectileMotionType.None, BattleProjectileImpactType.PrimaryTarget, BattleProjectileTargetMode.PrimaryTarget, 1, effects)
    {
    }

    public BattleSkillTimelineEventTemplate(Fix32 time, Fix32 energyGain, Fix32 projectileSpeed, BattleSkillEffectTemplate[] effects)
        : this(time, energyGain, projectileSpeed, projectileSpeed > Fix32.Zero ? BattleProjectileMotionType.Homing : BattleProjectileMotionType.None, BattleProjectileImpactType.PrimaryTarget, BattleProjectileTargetMode.PrimaryTarget, 1, effects)
    {
    }

    public BattleSkillTimelineEventTemplate(Fix32 time, Fix32 energyGain, Fix32 projectileSpeed, BattleProjectileMotionType projectileMotionType, BattleProjectileImpactType projectileImpactType, BattleSkillEffectTemplate[] effects)
        : this(time, energyGain, projectileSpeed, projectileMotionType, projectileImpactType, BattleProjectileTargetMode.PrimaryTarget, 1, effects)
    {
    }

    public BattleSkillTimelineEventTemplate(Fix32 time, Fix32 energyGain, Fix32 projectileSpeed, BattleProjectileMotionType projectileMotionType, BattleProjectileImpactType projectileImpactType, BattleProjectileTargetMode projectileTargetMode, int projectileTargetCount, BattleSkillEffectTemplate[] effects)
    {
        Time = time;
        EnergyGain = energyGain;
        ProjectileSpeed = projectileSpeed;
        ProjectileMotionType = projectileMotionType;
        ProjectileImpactType = projectileImpactType;
        ProjectileTargetMode = projectileTargetMode;
        ProjectileTargetCount = projectileTargetCount;
        Effects = effects ?? System.Array.Empty<BattleSkillEffectTemplate>();
    }

    public readonly Fix32 Time;
    public readonly Fix32 EnergyGain;
    public readonly Fix32 ProjectileSpeed;
    public readonly BattleProjectileMotionType ProjectileMotionType;
    public readonly BattleProjectileImpactType ProjectileImpactType;
    public readonly BattleProjectileTargetMode ProjectileTargetMode;
    public readonly int ProjectileTargetCount;
    public readonly BattleSkillEffectTemplate[] Effects;

    public BattleSkillTimelineEvent Build(BattleUnitSpec unitSpec)
    {
        BattleSkillEffectSpec[] effects = new BattleSkillEffectSpec[Effects.Length];
        for (int i = 0; i < Effects.Length; i++)
        {
            effects[i] = Effects[i].Build(unitSpec);
        }

        return new BattleSkillTimelineEvent(Time, EnergyGain, ProjectileSpeed, ProjectileMotionType, ProjectileImpactType, ProjectileTargetMode, ProjectileTargetCount, effects);
    }
}
