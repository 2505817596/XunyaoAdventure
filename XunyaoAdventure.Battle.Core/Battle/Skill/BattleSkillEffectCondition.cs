using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly struct BattleSkillEffectCondition
{
    public readonly BattleSkillEffectConditionType Type;
    public readonly Fix32 Threshold;
    public readonly BattleBuffType BuffType;
    public readonly BattleUnitTag UnitTags;

    public BattleSkillEffectCondition(
        BattleSkillEffectConditionType type,
        Fix32 threshold = default,
        BattleBuffType buffType = BattleBuffType.None,
        BattleUnitTag unitTags = BattleUnitTag.None)
    {
        Type = type;
        Threshold = threshold;
        BuffType = buffType;
        UnitTags = unitTags;
    }

    public bool IsEmpty => Type == BattleSkillEffectConditionType.None;

    public bool Matches(BattleUnit source, BattleUnit target)
    {
        return Type switch
        {
            BattleSkillEffectConditionType.None => true,
            BattleSkillEffectConditionType.SourceHpBelow => source.HpRatio < Threshold,
            BattleSkillEffectConditionType.SourceHpAbove => source.HpRatio > Threshold,
            BattleSkillEffectConditionType.TargetHpBelow => target.HpRatio < Threshold,
            BattleSkillEffectConditionType.TargetHpAbove => target.HpRatio > Threshold,
            BattleSkillEffectConditionType.SourceHasBuff => source.HasBuff(BuffType),
            BattleSkillEffectConditionType.TargetHasBuff => target.HasBuff(BuffType),
            BattleSkillEffectConditionType.SourceMissingBuff => !source.HasBuff(BuffType),
            BattleSkillEffectConditionType.TargetMissingBuff => !target.HasBuff(BuffType),
            BattleSkillEffectConditionType.TargetIsControlled => target.IsControlled,
            BattleSkillEffectConditionType.TargetIsNotControlled => !target.IsControlled,
            BattleSkillEffectConditionType.SourceHasUnitTag => source.HasUnitTag(UnitTags),
            BattleSkillEffectConditionType.TargetHasUnitTag => target.HasUnitTag(UnitTags),
            BattleSkillEffectConditionType.SourceMissingUnitTag => !source.HasUnitTag(UnitTags),
            BattleSkillEffectConditionType.TargetMissingUnitTag => !target.HasUnitTag(UnitTags),
            _ => false,
        };
    }

    public static BattleSkillEffectCondition SourceHpBelow(Fix32 hpRatio)
        => new(BattleSkillEffectConditionType.SourceHpBelow, hpRatio);

    public static BattleSkillEffectCondition TargetHpBelow(Fix32 hpRatio)
        => new(BattleSkillEffectConditionType.TargetHpBelow, hpRatio);

    public static BattleSkillEffectCondition TargetHasBuff(BattleBuffType buffType)
        => new(BattleSkillEffectConditionType.TargetHasBuff, buffType: buffType);

    public static BattleSkillEffectCondition TargetMissingBuff(BattleBuffType buffType)
        => new(BattleSkillEffectConditionType.TargetMissingBuff, buffType: buffType);

    public static BattleSkillEffectCondition TargetIsControlled()
        => new(BattleSkillEffectConditionType.TargetIsControlled);

    public static BattleSkillEffectCondition TargetHasUnitTag(BattleUnitTag unitTags)
        => new(BattleSkillEffectConditionType.TargetHasUnitTag, unitTags: unitTags);

    public static BattleSkillEffectCondition TargetMissingUnitTag(BattleUnitTag unitTags)
        => new(BattleSkillEffectConditionType.TargetMissingUnitTag, unitTags: unitTags);
}
