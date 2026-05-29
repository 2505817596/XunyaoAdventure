using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static partial class BattleConfigValidator
{
    private static BattleConfigValidationResult ValidateEffect(int profileId, BattleSkillSlot slot, int index, BattleSkillEffectTemplate effect)
        => ValidateEffect(profileId, slot.ToString(), index, effect);

    private static BattleConfigValidationResult ValidateEffect(int profileId, string effectGroup, int index, BattleSkillEffectTemplate effect)
    {
        if (effect.Type == BattleSkillEffectType.None)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has type None.");
        }

        if (!IsDefinedEffectTarget(effect.Target))
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has invalid target.");
        }

        if (!IsDefinedTargetTeam(effect.TargetTeam))
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has invalid target team.");
        }

        if (!IsDefinedDamageType(effect.DamageType))
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has invalid damage type.");
        }

        if (!IsDefinedSkillTags(effect.Tags))
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has invalid skill tags.");
        }

        if (!BattleSkillTagUtility.Has(effect.Tags, BattleSkillTag.Damage)
            && (BattleSkillTagUtility.Has(effect.Tags, BattleSkillTag.NoLifeSteal)
                || BattleSkillTagUtility.Has(effect.Tags, BattleSkillTag.NoReflection)))
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} no lifesteal/reflection tags require damage tag.");
        }

        if (!IsDefinedEffectCondition(effect.Condition))
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has invalid condition.");
        }

        if (effect.Condition.Type == BattleSkillEffectConditionType.SourceHpBelow
            || effect.Condition.Type == BattleSkillEffectConditionType.SourceHpAbove
            || effect.Condition.Type == BattleSkillEffectConditionType.TargetHpBelow
            || effect.Condition.Type == BattleSkillEffectConditionType.TargetHpAbove)
        {
            if (effect.Condition.Threshold <= Fix32.Zero || effect.Condition.Threshold > Fix32.One)
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} condition threshold is out of range.");
            }
        }

        if ((effect.Condition.Type == BattleSkillEffectConditionType.SourceHasBuff
                || effect.Condition.Type == BattleSkillEffectConditionType.TargetHasBuff
                || effect.Condition.Type == BattleSkillEffectConditionType.SourceMissingBuff
                || effect.Condition.Type == BattleSkillEffectConditionType.TargetMissingBuff)
            && effect.Condition.BuffType == BattleBuffType.None)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} condition buff type is invalid.");
        }

        if ((effect.Condition.Type == BattleSkillEffectConditionType.SourceHasUnitTag
                || effect.Condition.Type == BattleSkillEffectConditionType.TargetHasUnitTag
                || effect.Condition.Type == BattleSkillEffectConditionType.SourceMissingUnitTag
                || effect.Condition.Type == BattleSkillEffectConditionType.TargetMissingUnitTag)
            && effect.Condition.UnitTags == BattleUnitTag.None)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} condition unit tags are invalid.");
        }

        if (!IsDefinedValueSource(effect.AmountSource) || !IsDefinedValueSource(effect.RadiusSource) || !IsDefinedValueSource(effect.StatusDurationSource))
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has invalid value source.");
        }

        BattleConfigValidationResult formulaResult = ValidateFormula(profileId, effectGroup, index, effect.AmountFormula);
        if (!formulaResult.IsValid)
        {
            return formulaResult;
        }

        formulaResult = ValidateFormula(profileId, effectGroup, index, effect.RadiusFormula);
        if (!formulaResult.IsValid)
        {
            return formulaResult;
        }

        formulaResult = ValidateFormula(profileId, effectGroup, index, effect.StatusDurationFormula);
        if (!formulaResult.IsValid)
        {
            return formulaResult;
        }

        if (effect.Radius < Fix32.Zero || effect.StatusDuration < Fix32.Zero)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has negative radius or duration.");
        }

        if (RequiresPositiveRadius(effect.Target)
            && effect.RadiusSource == BattleSkillValueSource.Constant
            && effect.Radius <= Fix32.Zero)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} target requires positive radius, width, or jump radius.");
        }

        if (RequiresPositiveTargetCount(effect.Target) && effect.Value <= 0)
        {
            return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} chain target count must be positive.");
        }

        switch (effect.Type)
        {
            case BattleSkillEffectType.Damage:
            case BattleSkillEffectType.Heal:
                if (effect.BuffType != BattleBuffType.None)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} should not define buff type.");
                }
                break;
            case BattleSkillEffectType.ApplyStatus:
                if (!IsDefinedBuffType(effect.BuffType) || effect.BuffType == BattleBuffType.None)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has invalid buff type.");
                }
                if ((effect.BuffType == BattleBuffType.DamageOverTime || effect.BuffType == BattleBuffType.HealOverTime)
                    && ((effect.AmountSource == BattleSkillValueSource.Constant && effect.Amount <= Fix32.Zero)
                        || effect.Value <= 0
                        || effect.StatusDuration <= Fix32.Zero))
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} periodic buff requires positive amount, tick interval, and duration.");
                }
                if (RequiresPositiveDuration(effect.BuffType)
                    && effect.StatusDurationSource == BattleSkillValueSource.Constant
                    && effect.StatusDuration <= Fix32.Zero)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} buff {effect.BuffType} requires positive duration.");
                }
                if (RequiresPositiveModifierRate(effect.BuffType)
                    && effect.AmountSource == BattleSkillValueSource.Constant
                    && effect.Amount <= Fix32.Zero)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} buff {effect.BuffType} requires positive modifier rate.");
                }
                break;
            case BattleSkillEffectType.Dispel:
                if (effect.ResolveDispelCount() <= 0)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} dispel count must be positive.");
                }
                if (!IsDefinedDispelMode(effect.ResolveDispelMode()))
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has invalid dispel mode.");
                }
                break;
            case BattleSkillEffectType.ModifyEnergy:
                break;
            case BattleSkillEffectType.Displace:
                break;
            case BattleSkillEffectType.Interrupt:
                if (effect.BuffType != BattleBuffType.None)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} interrupt should not define buff type.");
                }
                break;
            case BattleSkillEffectType.Revive:
                if (effect.BuffType != BattleBuffType.Revive)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} revive must define revive buff.");
                }
                break;
            case BattleSkillEffectType.Summon:
                if (effect.Value < 0)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} summon profile id must be non-negative.");
                }
                if (effect.StatusDuration < Fix32.Zero)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} summon duration is negative.");
                }
                break;
            case BattleSkillEffectType.Execute:
                if (effect.BuffType != BattleBuffType.None)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} execute should not define buff type.");
                }
                if (effect.AmountSource == BattleSkillValueSource.Constant
                    && (effect.Amount <= Fix32.Zero || effect.Amount > Fix32.One))
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} execute hp threshold must be within (0, 1].");
                }
                if (effect.DamageType != BattleDamageType.Pure)
                {
                    return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} execute must use pure damage.");
                }
                break;
            default:
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} has invalid effect type.");
        }

        return BattleConfigValidationResult.Success();
    }

    private static BattleConfigValidationResult ValidateFormula(int profileId, string effectGroup, int index, BattleFormulaSpec formula)
    {
        if (formula.IsEmpty)
        {
            return BattleConfigValidationResult.Success();
        }

        for (int i = 0; i < formula.Terms.Length; i++)
        {
            BattleFormulaTerm term = formula.Terms[i];
            if (!IsDefinedValueSource(term.Source))
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} formula term {i} has invalid value source.");
            }

            if (!IsDefinedFormulaOp(term.Op))
            {
                return BattleConfigValidationResult.Fail($"Skill profile {profileId} {effectGroup} effect {index} formula term {i} has invalid op.");
            }
        }

        return BattleConfigValidationResult.Success();
    }
}

