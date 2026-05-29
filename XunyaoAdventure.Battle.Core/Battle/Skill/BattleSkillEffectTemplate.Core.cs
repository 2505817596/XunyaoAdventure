using LeanClr.Mathematics;



namespace LeanClr.Battle;



public readonly partial struct BattleSkillEffectTemplate

{


    public readonly BattleSkillEffectType Type;
    public readonly BattleSkillEffectTarget Target;
    public readonly BattleSkillValueSource AmountSource;
    public readonly Fix32 Amount;
    public readonly BattleSkillValueSource RadiusSource;
    public readonly Fix32 Radius;
    public readonly int Value;
    public readonly BattleBuffType BuffType;
    public readonly BattleSkillValueSource StatusDurationSource;
    public readonly Fix32 StatusDuration;
    public readonly BattleSkillTargetTeam TargetTeam;
    public readonly BattleDamageType DamageType;
    public readonly BattleFormulaSpec AmountFormula;
    public readonly BattleFormulaSpec RadiusFormula;
    public readonly BattleFormulaSpec StatusDurationFormula;
    public readonly BattleSkillTag Tags;
    public readonly BattleSkillEffectCondition Condition;

    public BattleSkillEffectTemplate(
        BattleSkillEffectType type,
        BattleSkillEffectTarget target,
        BattleSkillValueSource amountSource,
        Fix32 amount,
        BattleSkillValueSource radiusSource,
        Fix32 radius,
        int value,
        BattleBuffType buffType,
        BattleSkillValueSource statusDurationSource,
        Fix32 statusDuration,
        BattleSkillTargetTeam targetTeam,
        BattleDamageType damageType = BattleDamageType.Physical,
        BattleFormulaSpec amountFormula = default,
        BattleFormulaSpec radiusFormula = default,
        BattleFormulaSpec statusDurationFormula = default,
        BattleSkillTag tags = BattleSkillTag.None,
        BattleSkillEffectCondition condition = default)
    {
        Type = type;
        Target = target;
        AmountSource = amountSource;
        Amount = amount;
        RadiusSource = radiusSource;
        Radius = radius;
        Value = value;
        BuffType = buffType;
        StatusDurationSource = statusDurationSource;
        StatusDuration = statusDuration;
        TargetTeam = targetTeam;
        DamageType = damageType;
        AmountFormula = amountFormula;
        RadiusFormula = radiusFormula;
        StatusDurationFormula = statusDurationFormula;
        Tags = tags | BattleSkillTagUtility.InferEffectTags(type, buffType);
        Condition = condition;
    }

    public BattleSkillEffectSpec Build(BattleUnitSpec unitSpec)
    {
        return new BattleSkillEffectSpec(
            Type,
            Target,
            ResolveValue(unitSpec, AmountSource, Amount),
            ResolveValue(unitSpec, RadiusSource, Radius),
            Value,
            BuffType,
            ResolveValue(unitSpec, StatusDurationSource, StatusDuration),
            TargetTeam,
            DamageType,
            AmountFormula,
            RadiusFormula,
            StatusDurationFormula,
            Tags,
            Condition);
    }

    public BattleSkillEffectTemplate WithTags(BattleSkillTag tags)
        => new(
            Type,
            Target,
            AmountSource,
            Amount,
            RadiusSource,
            Radius,
            Value,
            BuffType,
            StatusDurationSource,
            StatusDuration,
            TargetTeam,
            DamageType,
            AmountFormula,
            RadiusFormula,
            StatusDurationFormula,
            tags,
            Condition);

    public BattleSkillEffectTemplate WithCondition(BattleSkillEffectCondition condition)
        => new(
            Type,
            Target,
            AmountSource,
            Amount,
            RadiusSource,
            Radius,
            Value,
            BuffType,
            StatusDurationSource,
            StatusDuration,
            TargetTeam,
            DamageType,
            AmountFormula,
            RadiusFormula,
            StatusDurationFormula,
            Tags,
            condition);

    public BattleSkillEffectTemplate OnlyIfTargetHpBelow(Fix32 hpRatio)
        => WithCondition(BattleSkillEffectCondition.TargetHpBelow(hpRatio));

    public BattleSkillEffectTemplate OnlyIfTargetHasBuff(BattleBuffType buffType)
        => WithCondition(BattleSkillEffectCondition.TargetHasBuff(buffType));

    public BattleSkillEffectTemplate OnlyIfTargetIsControlled()
        => WithCondition(BattleSkillEffectCondition.TargetIsControlled());

    public BattleSkillEffectTemplate OnlyIfTargetHasUnitTag(BattleUnitTag unitTags)
        => WithCondition(BattleSkillEffectCondition.TargetHasUnitTag(unitTags));

    public BattleSkillEffectTemplate ExcludingTargetUnitTag(BattleUnitTag unitTags)
        => WithCondition(BattleSkillEffectCondition.TargetMissingUnitTag(unitTags));

    public BattleSkillEffectTemplate WithoutLifeSteal()
        => WithTags(Tags | BattleSkillTag.NoLifeSteal);

    public BattleSkillEffectTemplate WithoutReflection()
        => WithTags(Tags | BattleSkillTag.NoReflection);

    public BattleSkillEffectTemplate WithoutLifeStealOrReflection()
        => WithTags(Tags | BattleSkillTag.NoLifeSteal | BattleSkillTag.NoReflection);

    public int ResolveDispelCount()
        => BattleSkillEffectDispelValue.ResolveCount(Value);

    public BattleDispelMode ResolveDispelMode()
        => BattleSkillEffectDispelValue.ResolveMode(Value);

    private static Fix32 ResolveValue(BattleUnitSpec unitSpec, BattleSkillValueSource source, Fix32 constant)
    {
        return source switch
        {
            BattleSkillValueSource.PhysicalAttack => unitSpec.PhysicalAttack,
            BattleSkillValueSource.UltimateDamage => unitSpec.UltimateDamage,
            BattleSkillValueSource.UltimateRadius => unitSpec.UltimateRadius,
            BattleSkillValueSource.SourceMagicPower => unitSpec.MagicPower,
            BattleSkillValueSource.SourcePhysicalCrit => unitSpec.PhysicalCrit,
            _ => constant,
        };
    }
}
