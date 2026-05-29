using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly struct BattleFormulaTerm
{
    public readonly BattleSkillValueSource Source;
    public readonly Fix32 Coefficient;
    public readonly Fix32 Constant;
    public readonly BattleFormulaOp Op;

    public BattleFormulaTerm(BattleSkillValueSource source, Fix32 coefficient, Fix32 constant, BattleFormulaOp op = BattleFormulaOp.Add)
    {
        Source = source;
        Coefficient = coefficient;
        Constant = constant;
        Op = op;
    }

    public static BattleFormulaTerm ConstantValue(Fix32 value)
        => new(BattleSkillValueSource.Constant, Fix32.Zero, value);

    public static BattleFormulaTerm SourceValue(BattleSkillValueSource source, Fix32 coefficient)
        => new(source, coefficient, Fix32.Zero);
}

public readonly struct BattleFormulaSpec
{
    public readonly BattleFormulaTerm[] Terms;

    public BattleFormulaSpec(BattleFormulaTerm[] terms)
    {
        Terms = terms ?? System.Array.Empty<BattleFormulaTerm>();
    }

    public bool IsEmpty => Terms == null || Terms.Length == 0;

    public static BattleFormulaSpec Constant(Fix32 value)
        => new(new[] { BattleFormulaTerm.ConstantValue(value) });

    public static BattleFormulaSpec Source(BattleSkillValueSource source)
        => new(new[] { BattleFormulaTerm.SourceValue(source, Fix32.One) });

    public static BattleFormulaSpec Source(BattleSkillValueSource source, Fix32 coefficient)
        => new(new[] { BattleFormulaTerm.SourceValue(source, coefficient) });

    public Fix32 Evaluate(BattleUnit source, BattleUnit target, Fix32 fallback)
    {
        if (Terms == null || Terms.Length == 0)
        {
            return fallback;
        }

        Fix32 result = Fix32.Zero;
        for (int i = 0; i < Terms.Length; i++)
        {
            BattleFormulaTerm term = Terms[i];
            Fix32 value = ResolveValue(source, target, term.Source) * term.Coefficient + term.Constant;
            if (term.Op == BattleFormulaOp.Multiply)
            {
                result *= value;
            }
            else
            {
                result += value;
            }
        }

        return result;
    }

    private static Fix32 ResolveValue(BattleUnit source, BattleUnit target, BattleSkillValueSource valueSource)
    {
        return valueSource switch
        {
            BattleSkillValueSource.PhysicalAttack => source.PhysicalAttack,
            BattleSkillValueSource.UltimateDamage => source.Spec.UltimateDamage,
            BattleSkillValueSource.UltimateRadius => source.Spec.UltimateRadius,
            BattleSkillValueSource.SourceMaxHp => source.Spec.MaxHp,
            BattleSkillValueSource.SourceCurrentHp => source.Hp,
            BattleSkillValueSource.SourceMissingHp => source.Spec.MaxHp - source.Hp,
            BattleSkillValueSource.SourceHpRatio => source.Spec.MaxHp > Fix32.Zero ? source.Hp / source.Spec.MaxHp : Fix32.Zero,
            BattleSkillValueSource.SourceEnergy => source.Energy,
            BattleSkillValueSource.TargetMaxHp => target.Spec.MaxHp,
            BattleSkillValueSource.TargetCurrentHp => target.Hp,
            BattleSkillValueSource.TargetMissingHp => target.Spec.MaxHp - target.Hp,
            BattleSkillValueSource.TargetHpRatio => target.Spec.MaxHp > Fix32.Zero ? target.Hp / target.Spec.MaxHp : Fix32.Zero,
            BattleSkillValueSource.TargetEnergy => target.Energy,
            BattleSkillValueSource.SourcePhysicalArmor => source.PhysicalArmor,
            BattleSkillValueSource.SourceMagicResist => source.MagicResist,
            BattleSkillValueSource.TargetPhysicalArmor => target.PhysicalArmor,
            BattleSkillValueSource.TargetMagicResist => target.MagicResist,
            BattleSkillValueSource.SourceMagicPower => source.MagicPower,
            BattleSkillValueSource.SourcePhysicalCrit => source.PhysicalCrit,
            BattleSkillValueSource.TargetPhysicalCrit => target.PhysicalCrit,
            _ => Fix32.One,
        };
    }
}
