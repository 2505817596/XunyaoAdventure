using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static partial class BattleConfigValidator
{
    internal static BattleConfigValidationResult ValidateSkillProfile(int expectedId, BattleSkillProfile profile)
    {
        if (profile.Id != expectedId)
        {
            return BattleConfigValidationResult.Fail($"Skill profile id mismatch. expected={expectedId} actual={profile.Id}");
        }

        if (profile.BasicAttackWindupRatio < Fix32.Zero)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profile.Id} basic windup ratio is negative.");
        }

        if (profile.BasicAttackMinWindup < Fix32.Zero || profile.BasicAttackMaxWindup < Fix32.Zero)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profile.Id} basic windup clamp is negative.");
        }

        if (profile.BasicAttackMinWindup > profile.BasicAttackMaxWindup)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profile.Id} basic windup min is greater than max.");
        }

        if (profile.BasicAttackEnergyGainOnHit < Fix32.Zero)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profile.Id} basic energy gain is negative.");
        }

        if (profile.BasicAttackProjectileSpeed < Fix32.Zero)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profile.Id} basic projectile speed is negative.");
        }

        if (!IsDefinedProjectileMotionType(profile.BasicAttackProjectileMotionType))
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profile.Id} basic projectile motion type is invalid.");
        }

        if (profile.BasicAttackProjectileSpeed == Fix32.Zero && profile.BasicAttackProjectileMotionType != BattleProjectileMotionType.None)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profile.Id} basic projectile motion type requires speed.");
        }

        if (profile.UltimateWindupTime < Fix32.Zero || profile.UltimateRecoverTime < Fix32.Zero)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profile.Id} ultimate timing is negative.");
        }

        BattleConfigValidationResult basicResult = ValidateEffects(profile.Id, BattleSkillSlot.BasicAttack, profile.BasicAttackEffects);
        if (!basicResult.IsValid)
        {
            return basicResult;
        }

        BattleConfigValidationResult ultimateResult = ValidateEffects(profile.Id, BattleSkillSlot.Ultimate, profile.UltimateEffects);
        if (!ultimateResult.IsValid)
        {
            return ultimateResult;
        }

        BattleConfigValidationResult deathResult = ValidateOptionalEffects(profile.Id, "death", profile.DeathEffects);
        if (!deathResult.IsValid)
        {
            return deathResult;
        }

        BattleConfigValidationResult passiveResult = ValidatePassiveTriggers(profile.Id, profile.PassiveTriggers);
        if (!passiveResult.IsValid)
        {
            return passiveResult;
        }

        BattleConfigValidationResult autoResult = ValidateAutoSkills(profile.Id, profile.AutoSkillTemplates);
        if (!autoResult.IsValid)
        {
            return autoResult;
        }

        return ValidateTimelineEvents(profile.Id, profile.UltimateWindupTime + profile.UltimateRecoverTime, profile.UltimateTimelineEvents);
    }

    internal static BattleConfigValidationResult ValidateSkillSpec(BattleSkillSpec skill)
    {
        if (skill.WindupTime < Fix32.Zero || skill.RecoverTime < Fix32.Zero)
        {
            return BattleConfigValidationResult.Fail($"Skill {skill.Type} has negative timing.");
        }

        if (skill.TimelineEvents.Length == 0)
        {
            return BattleConfigValidationResult.Fail($"Skill {skill.Type} has no timeline events.");
        }

        Fix32 totalTime = skill.WindupTime + skill.RecoverTime;
        Fix32 previousTime = Fix32.Zero;
        for (int i = 0; i < skill.TimelineEvents.Length; i++)
        {
            BattleSkillTimelineEvent timelineEvent = skill.TimelineEvents[i];
            if (timelineEvent.Time < Fix32.Zero || timelineEvent.Time > totalTime)
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline event {i} time is out of range.");
            }

            if (i > 0 && timelineEvent.Time < previousTime)
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline events must be sorted.");
            }

            if (timelineEvent.EnergyGain < Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline event {i} energy gain is negative.");
            }

            if (timelineEvent.ProjectileSpeed < Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline event {i} projectile speed is negative.");
            }

            if (!IsDefinedProjectileMotionType(timelineEvent.ProjectileMotionType))
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline event {i} projectile motion type is invalid.");
            }

            if (!IsDefinedProjectileImpactType(timelineEvent.ProjectileImpactType))
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline event {i} projectile impact type is invalid.");
            }

            if (!IsDefinedProjectileTargetMode(timelineEvent.ProjectileTargetMode))
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline event {i} projectile target mode is invalid.");
            }

            if (timelineEvent.ProjectileSpeed == Fix32.Zero && timelineEvent.ProjectileMotionType != BattleProjectileMotionType.None)
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline event {i} projectile motion type requires speed.");
            }

            if (timelineEvent.ProjectileTargetMode != BattleProjectileTargetMode.PrimaryTarget && timelineEvent.ProjectileSpeed <= Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline event {i} projectile target mode requires speed.");
            }

            if (timelineEvent.ProjectileTargetCount <= 0)
            {
                return BattleConfigValidationResult.Fail($"Skill {skill.Type} timeline event {i} projectile target count must be positive.");
            }

            previousTime = timelineEvent.Time;
        }

        return BattleConfigValidationResult.Success();
    }

    private static BattleConfigValidationResult ValidateEffects(int profileId, BattleSkillSlot slot, BattleSkillEffectTemplate[] effects)
        => ValidateEffects(profileId, slot.ToString(), effects);

    private static BattleConfigValidationResult ValidateEffects(int profileId, string groupName, BattleSkillEffectTemplate[] effects)
    {
        if (effects == null)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {groupName} effects is null.");
        }

        if (effects.Length == 0)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {groupName} has no effects.");
        }

        for (int i = 0; i < effects.Length; i++)
        {
            BattleConfigValidationResult result = ValidateEffect(profileId, groupName, i, effects[i]);
            if (!result.IsValid)
            {
                return result;
            }
        }

        return BattleConfigValidationResult.Success();
    }

    private static BattleConfigValidationResult ValidateOptionalEffects(int profileId, string groupName, BattleSkillEffectTemplate[] effects)
    {
        if (effects == null)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {groupName} effects is null.");
        }

        for (int i = 0; i < effects.Length; i++)
        {
            BattleConfigValidationResult result = ValidateEffect(profileId, groupName, i, effects[i]);
            if (!result.IsValid)
            {
                return result;
            }
        }

        return BattleConfigValidationResult.Success();
    }

    private static BattleConfigValidationResult ValidateTimelineEvents(int profileId, Fix32 totalTime, BattleSkillTimelineEventTemplate[] timelineEvents)
    {
        if (timelineEvents == null || timelineEvents.Length == 0)
        {
            return BattleConfigValidationResult.Success();
        }

        Fix32 previousTime = Fix32.Zero;
        for (int i = 0; i < timelineEvents.Length; i++)
        {
            BattleSkillTimelineEventTemplate timelineEvent = timelineEvents[i];
            if (timelineEvent.Time < Fix32.Zero || timelineEvent.Time > totalTime)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline event {i} time is out of range.");
            }

            if (i > 0 && timelineEvent.Time < previousTime)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline events must be sorted.");
            }

            if (timelineEvent.EnergyGain < Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline event {i} energy gain is negative.");
            }

            if (timelineEvent.ProjectileSpeed < Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline event {i} projectile speed is negative.");
            }

            if (!IsDefinedProjectileMotionType(timelineEvent.ProjectileMotionType))
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline event {i} projectile motion type is invalid.");
            }

            if (!IsDefinedProjectileImpactType(timelineEvent.ProjectileImpactType))
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline event {i} projectile impact type is invalid.");
            }

            if (!IsDefinedProjectileTargetMode(timelineEvent.ProjectileTargetMode))
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline event {i} projectile target mode is invalid.");
            }

            if (timelineEvent.ProjectileSpeed == Fix32.Zero && timelineEvent.ProjectileMotionType != BattleProjectileMotionType.None)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline event {i} projectile motion type requires speed.");
            }

            if (timelineEvent.ProjectileTargetMode != BattleProjectileTargetMode.PrimaryTarget && timelineEvent.ProjectileSpeed <= Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline event {i} projectile target mode requires speed.");
            }

            if (timelineEvent.ProjectileTargetCount <= 0)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} timeline event {i} projectile target count must be positive.");
            }

            BattleConfigValidationResult effectsResult = ValidateEffects(profileId, BattleSkillSlot.Ultimate, timelineEvent.Effects);
            if (!effectsResult.IsValid)
            {
                return effectsResult;
            }

            previousTime = timelineEvent.Time;
        }

        return BattleConfigValidationResult.Success();
    }
}

