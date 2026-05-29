using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly struct BattleSkillSpec
{
    public readonly BattleSkillSlot Slot;
    public readonly BattleSkillType Type;
    public readonly Fix32 Range;
    public readonly Fix32 EffectRadius;
    public readonly Fix32 Damage;
    public readonly Fix32 WindupTime;
    public readonly Fix32 RecoverTime;
    public readonly Fix32 EnergyCost;
    public readonly Fix32 Cooldown;
    public readonly Fix32 EnergyGainOnHit;
    public readonly bool RequiresRangeCheck;
    public readonly BattleSkillTargetTeam TargetTeam;
    public readonly BattleSkillEffectSpec[] Effects;
    public readonly BattleSkillTimelineEvent[] TimelineEvents;

    public BattleSkillSpec(
        BattleSkillSlot slot,
        BattleSkillType type,
        Fix32 range,
        Fix32 effectRadius,
        Fix32 damage,
        Fix32 windupTime,
        Fix32 recoverTime,
        Fix32 energyCost,
        Fix32 cooldown,
        Fix32 energyGainOnHit,
        bool requiresRangeCheck,
        BattleSkillTargetTeam targetTeam,
        BattleSkillEffectSpec[] effects,
        BattleSkillTimelineEvent[] timelineEvents)
    {
        Slot = slot;
        Type = type;
        Range = range;
        EffectRadius = effectRadius;
        Damage = damage;
        WindupTime = windupTime;
        RecoverTime = recoverTime;
        EnergyCost = energyCost;
        Cooldown = cooldown;
        EnergyGainOnHit = energyGainOnHit;
        RequiresRangeCheck = requiresRangeCheck;
        TargetTeam = targetTeam;
        Effects = effects ?? System.Array.Empty<BattleSkillEffectSpec>();
        TimelineEvents = timelineEvents ?? System.Array.Empty<BattleSkillTimelineEvent>();
    }

    public BattleSkillSpec(BattleSkillConfig config)
        : this(
            config.Slot,
            config.Type,
            config.Range,
            config.EffectRadius,
            config.Damage,
            config.WindupTime,
            config.RecoverTime,
            config.EnergyCost,
            config.Cooldown,
            config.EnergyGainOnHit,
            config.RequiresRangeCheck,
            config.TargetTeam,
            config.Effects,
            config.TimelineEvents)
    {
    }

    public static BattleSkillSpec FromConfig(BattleSkillConfig config) => new(config);

    public static BattleSkillConfig CreateBasicAttackConfig(BattleUnitSpec unitSpec)
        => CreateBasicAttackConfig(unitSpec, BattleSkillProfile.CreateDefault());

    public static BattleSkillConfig CreateBasicAttackConfig(BattleUnitSpec unitSpec, BattleSkillProfile profile)
    {
        Fix32 windup = unitSpec.AttackInterval * profile.BasicAttackWindupRatio;
        if (windup < profile.BasicAttackMinWindup)
        {
            windup = profile.BasicAttackMinWindup;
        }

        if (windup > profile.BasicAttackMaxWindup)
        {
            windup = profile.BasicAttackMaxWindup;
        }

        if (windup > unitSpec.AttackInterval)
        {
            windup = unitSpec.AttackInterval;
        }

        Fix32 recover = unitSpec.AttackInterval - windup;
        if (recover < Fix32.Zero)
        {
            recover = Fix32.Zero;
        }

        return new BattleSkillConfig(
            BattleSkillSlot.BasicAttack,
            BattleSkillType.BasicAttack,
            unitSpec.AttackRange,
            Fix32.Zero,
            unitSpec.PhysicalAttack,
            windup,
            recover,
            Fix32.Zero,
            Fix32.Zero,
            profile.BasicAttackEnergyGainOnHit,
            true,
            profile.BasicAttackTargetTeam,
            BuildEffects(unitSpec, profile.BasicAttackEffects),
            new[]
            {
                new BattleSkillTimelineEvent(
                    windup,
                    profile.BasicAttackEnergyGainOnHit,
                    profile.BasicAttackProjectileSpeed,
                    profile.BasicAttackProjectileMotionType,
                    BattleProjectileImpactType.PrimaryTarget,
                    BattleProjectileTargetMode.PrimaryTarget,
                    1,
                    System.Array.Empty<BattleSkillEffectSpec>()),
            });
    }

    public static BattleSkillConfig CreateUltimateConfig(BattleUnitSpec unitSpec)
        => CreateUltimateConfig(unitSpec, BattleSkillProfile.CreateDefault());

    public static BattleSkillConfig CreateUltimateConfig(BattleUnitSpec unitSpec, BattleSkillProfile profile)
    {
        return new BattleSkillConfig(
            BattleSkillSlot.Ultimate,
            BattleSkillType.Ultimate,
            unitSpec.AggroRange,
            unitSpec.UltimateRadius,
            unitSpec.UltimateDamage,
            profile.UltimateWindupTime,
            profile.UltimateRecoverTime,
            unitSpec.UltimateEnergyCost,
            unitSpec.UltimateCooldown,
            Fix32.Zero,
            false,
            profile.UltimateTargetTeam,
            BuildEffects(unitSpec, profile.UltimateEffects),
            BuildTimelineEvents(unitSpec, profile.UltimateTimelineEvents));
    }

    public static BattleSkillConfig CreateAutoSkillConfig(BattleUnitSpec unitSpec, BattleSkillConfigTemplate template)
    {
        return new BattleSkillConfig(
            template.Slot,
            template.Type,
            template.Range > Fix32.Zero ? template.Range : unitSpec.AggroRange,
            unitSpec.UltimateRadius,
            unitSpec.UltimateDamage,
            template.WindupTime,
            template.RecoverTime,
            Fix32.Zero,
            template.Cooldown,
            Fix32.Zero,
            template.RequiresRangeCheck,
            template.TargetTeam,
            BuildEffects(unitSpec, template.Effects),
            BuildTimelineEvents(unitSpec, template.TimelineEvents));
    }

    private static BattleSkillEffectSpec[] BuildEffects(BattleUnitSpec unitSpec, BattleSkillEffectTemplate[] templates)
    {
        BattleSkillEffectSpec[] effects = new BattleSkillEffectSpec[templates.Length];
        for (int i = 0; i < templates.Length; i++)
        {
            effects[i] = templates[i].Build(unitSpec);
        }

        return effects;
    }

    private static BattleSkillTimelineEvent[]? BuildTimelineEvents(BattleUnitSpec unitSpec, BattleSkillTimelineEventTemplate[] templates)
    {
        if (templates.Length == 0)
        {
            return null;
        }

        BattleSkillTimelineEvent[] timelineEvents = new BattleSkillTimelineEvent[templates.Length];
        for (int i = 0; i < templates.Length; i++)
        {
            timelineEvents[i] = templates[i].Build(unitSpec);
        }

        return timelineEvents;
    }
}
