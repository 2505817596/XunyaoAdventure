using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static partial class BattleConfigValidator
{
    private static BattleConfigValidationResult ValidatePassiveTriggers(int profileId, BattlePassiveTriggerTemplate[] triggers)
    {
        if (triggers == null)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive triggers is null.");
        }

        for (int i = 0; i < triggers.Length; i++)
        {
            BattlePassiveTriggerTemplate trigger = triggers[i];
            if (!IsDefinedPassiveTriggerCondition(trigger.Condition))
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} has invalid condition.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.HealthBelow && (trigger.Threshold <= Fix32.Zero || trigger.Threshold > Fix32.One))
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} health threshold is out of range.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.BattleStart && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} battle start threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.KillEnemy && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} kill enemy threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.DamageTaken && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} damage taken threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.DamageDealt && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} damage dealt threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.AllyDied && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} ally died threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.EnemyDied && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} enemy died threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.SkillStart && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} skill start threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.SkillFinish && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} skill finish threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.HealReceived && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} heal received threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.HealGranted && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} heal granted threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.SkillHit && trigger.Threshold != Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} skill hit threshold must be zero.");
            }

            if (trigger.Condition == BattlePassiveTriggerCondition.AuraTick && trigger.Threshold <= Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} passive trigger {i} aura tick radius must be positive.");
            }

            BattleConfigValidationResult effectsResult = ValidateEffects(profileId, $"passive trigger {i}", trigger.Effects);
            if (!effectsResult.IsValid)
            {
                return effectsResult;
            }
        }

        return BattleConfigValidationResult.Success();
    }

    private static BattleConfigValidationResult ValidateAutoSkills(int profileId, BattleSkillConfigTemplate[] autoSkills)
    {
        if (autoSkills == null)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} auto skills is null.");
        }

        for (int i = 0; i < autoSkills.Length; i++)
        {
            BattleSkillConfigTemplate skill = autoSkills[i];
            if (skill.Slot != BattleSkillSlot.AutoSkill1 && skill.Slot != BattleSkillSlot.AutoSkill2)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} auto skill {i} has invalid slot.");
            }

            if (skill.Type != BattleSkillType.AutoSkill)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} auto skill {i} must have AutoSkill type.");
            }

            if (!IsDefinedTargetTeam(skill.TargetTeam))
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} auto skill {i} has invalid target team.");
            }

            if (skill.Range < Fix32.Zero || skill.WindupTime < Fix32.Zero || skill.RecoverTime < Fix32.Zero || skill.Cooldown <= Fix32.Zero)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} auto skill {i} has invalid timing, range, or cooldown.");
            }

            BattleConfigValidationResult effectsResult = ValidateEffects(profileId, $"auto skill {i}", skill.Effects);
            if (!effectsResult.IsValid)
            {
                return effectsResult;
            }

            BattleConfigValidationResult timelineResult = ValidateTimelineEvents(profileId, skill.WindupTime + skill.RecoverTime, skill.TimelineEvents);
            if (!timelineResult.IsValid)
            {
                return timelineResult;
            }
        }

        return BattleConfigValidationResult.Success();
    }
}

