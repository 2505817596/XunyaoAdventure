using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly struct BattleSkillProfile
{
    public readonly int Id;
    public readonly Fix32 BasicAttackWindupRatio;
    public readonly Fix32 BasicAttackMinWindup;
    public readonly Fix32 BasicAttackMaxWindup;
    public readonly Fix32 BasicAttackEnergyGainOnHit;
    public readonly Fix32 BasicAttackProjectileSpeed;
    public readonly BattleProjectileMotionType BasicAttackProjectileMotionType;
    public readonly Fix32 UltimateWindupTime;
    public readonly Fix32 UltimateRecoverTime;
    public readonly BattleSkillTargetTeam BasicAttackTargetTeam;
    public readonly BattleSkillTargetTeam UltimateTargetTeam;
    public readonly BattleSkillEffectTemplate[] BasicAttackEffects;
    public readonly BattleSkillEffectTemplate[] UltimateEffects;
    public readonly BattleSkillTimelineEventTemplate[] UltimateTimelineEvents;
    public readonly BattleSkillConfigTemplate[] AutoSkillTemplates;
    public readonly BattleSkillEffectTemplate[] DeathEffects;
    public readonly BattlePassiveTriggerTemplate[] PassiveTriggers;
    public readonly BattleUnitTag UnitTags;

    public BattleSkillProfile(
        int id,
        Fix32 basicAttackWindupRatio,
        Fix32 basicAttackMinWindup,
        Fix32 basicAttackMaxWindup,
        Fix32 basicAttackEnergyGainOnHit,
        Fix32 basicAttackProjectileSpeed,
        BattleProjectileMotionType basicAttackProjectileMotionType,
        Fix32 ultimateWindupTime,
        Fix32 ultimateRecoverTime,
        BattleSkillTargetTeam basicAttackTargetTeam,
        BattleSkillTargetTeam ultimateTargetTeam,
        BattleSkillEffectTemplate[] basicAttackEffects,
        BattleSkillEffectTemplate[] ultimateEffects,
        BattleSkillTimelineEventTemplate[]? ultimateTimelineEvents = null,
        BattleSkillConfigTemplate[]? autoSkillTemplates = null,
        BattleSkillEffectTemplate[]? deathEffects = null,
        BattlePassiveTriggerTemplate[]? passiveTriggers = null,
        BattleUnitTag unitTags = BattleUnitTag.None)
    {
        Id = id;
        BasicAttackWindupRatio = basicAttackWindupRatio;
        BasicAttackMinWindup = basicAttackMinWindup;
        BasicAttackMaxWindup = basicAttackMaxWindup;
        BasicAttackEnergyGainOnHit = basicAttackEnergyGainOnHit;
        BasicAttackProjectileSpeed = basicAttackProjectileSpeed;
        BasicAttackProjectileMotionType = basicAttackProjectileMotionType;
        UltimateWindupTime = ultimateWindupTime;
        UltimateRecoverTime = ultimateRecoverTime;
        BasicAttackTargetTeam = basicAttackTargetTeam;
        UltimateTargetTeam = ultimateTargetTeam;
        BasicAttackEffects = basicAttackEffects ?? System.Array.Empty<BattleSkillEffectTemplate>();
        UltimateEffects = ultimateEffects ?? System.Array.Empty<BattleSkillEffectTemplate>();
        UltimateTimelineEvents = ultimateTimelineEvents ?? System.Array.Empty<BattleSkillTimelineEventTemplate>();
        AutoSkillTemplates = autoSkillTemplates ?? System.Array.Empty<BattleSkillConfigTemplate>();
        DeathEffects = deathEffects ?? System.Array.Empty<BattleSkillEffectTemplate>();
        PassiveTriggers = passiveTriggers ?? System.Array.Empty<BattlePassiveTriggerTemplate>();
        UnitTags = unitTags;
    }

    public static BattleSkillProfile CreateDefault()
    {
        return new BattleSkillProfile(
            0,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
                BattleSkillEffectTemplate.StunPrimary(Fix32.Half),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageEnemiesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            autoSkillTemplates: System.Array.Empty<BattleSkillConfigTemplate>(),
            deathEffects: System.Array.Empty<BattleSkillEffectTemplate>(),
            passiveTriggers: System.Array.Empty<BattlePassiveTriggerTemplate>());
    }
}

public readonly struct BattleSkillConfigTemplate
{
    public readonly BattleSkillSlot Slot;
    public readonly BattleSkillType Type;
    public readonly BattleSkillTargetTeam TargetTeam;
    public readonly Fix32 Range;
    public readonly Fix32 WindupTime;
    public readonly Fix32 RecoverTime;
    public readonly Fix32 Cooldown;
    public readonly bool RequiresRangeCheck;
    public readonly BattleSkillEffectTemplate[] Effects;
    public readonly BattleSkillTimelineEventTemplate[] TimelineEvents;

    public BattleSkillConfigTemplate(
        BattleSkillSlot slot,
        BattleSkillType type,
        BattleSkillTargetTeam targetTeam,
        Fix32 range,
        Fix32 windupTime,
        Fix32 recoverTime,
        Fix32 cooldown,
        bool requiresRangeCheck,
        BattleSkillEffectTemplate[] effects,
        BattleSkillTimelineEventTemplate[]? timelineEvents = null)
    {
        Slot = slot;
        Type = type;
        TargetTeam = targetTeam;
        Range = range;
        WindupTime = windupTime;
        RecoverTime = recoverTime;
        Cooldown = cooldown;
        RequiresRangeCheck = requiresRangeCheck;
        Effects = effects ?? System.Array.Empty<BattleSkillEffectTemplate>();
        TimelineEvents = timelineEvents ?? System.Array.Empty<BattleSkillTimelineEventTemplate>();
    }
}

public enum BattlePassiveTriggerCondition
{
    None = 0,
    HealthBelow = 1,
    BattleStart = 2,
    KillEnemy = 3,
    DamageTaken = 4,
    DamageDealt = 5,
    AllyDied = 6,
    EnemyDied = 7,
    SkillStart = 8,
    SkillFinish = 9,
    HealReceived = 10,
    HealGranted = 11,
    SkillHit = 12,
    AuraTick = 13,
}

public readonly struct BattlePassiveTriggerTemplate
{
    public readonly BattlePassiveTriggerCondition Condition;
    public readonly Fix32 Threshold;
    public readonly BattleSkillEffectTemplate[] Effects;
    public readonly bool ConsumeOnTrigger;

    public BattlePassiveTriggerTemplate(
        BattlePassiveTriggerCondition condition,
        Fix32 threshold,
        BattleSkillEffectTemplate[] effects,
        bool consumeOnTrigger = true)
    {
        Condition = condition;
        Threshold = threshold;
        Effects = effects ?? System.Array.Empty<BattleSkillEffectTemplate>();
        ConsumeOnTrigger = consumeOnTrigger;
    }

    public static BattlePassiveTriggerTemplate HealthBelow(Fix32 hpRatio, BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.HealthBelow, hpRatio, effects);

    public static BattlePassiveTriggerTemplate BattleStart(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.BattleStart, Fix32.Zero, effects);

    public static BattlePassiveTriggerTemplate KillEnemy(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.KillEnemy, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate DamageTaken(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.DamageTaken, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate DamageDealt(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.DamageDealt, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate AllyDied(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.AllyDied, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate EnemyDied(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.EnemyDied, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate SkillStart(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.SkillStart, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate SkillFinish(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.SkillFinish, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate HealReceived(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.HealReceived, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate HealGranted(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.HealGranted, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate SkillHit(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.SkillHit, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate AuraTick(BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.AuraTick, Fix32.Zero, effects, false);

    public static BattlePassiveTriggerTemplate AuraTick(Fix32 radius, BattleSkillEffectTemplate[] effects)
        => new(BattlePassiveTriggerCondition.AuraTick, radius, effects, false);
}
