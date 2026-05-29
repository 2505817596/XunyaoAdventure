using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly struct BattleSkillConfig
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

    public BattleSkillConfig(
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
        BattleSkillTimelineEvent[]? timelineEvents = null)
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
        TimelineEvents = timelineEvents ?? new[]
        {
            new BattleSkillTimelineEvent(windupTime, energyGainOnHit),
        };
    }
}
