namespace LeanClr.Battle;

public readonly struct BattleSkillLoadout
{
    public readonly BattleSkillSpec BasicAttack;
    public readonly BattleSkillSpec Ultimate;
    public readonly BattleSkillSpec AutoSkill1;
    public readonly BattleSkillSpec AutoSkill2;

    public BattleSkillLoadout(BattleSkillSpec basicAttack, BattleSkillSpec ultimate)
        : this(basicAttack, ultimate, default, default)
    {
    }

    public BattleSkillLoadout(BattleSkillSpec basicAttack, BattleSkillSpec ultimate, BattleSkillSpec autoSkill1, BattleSkillSpec autoSkill2)
    {
        BasicAttack = basicAttack;
        Ultimate = ultimate;
        AutoSkill1 = autoSkill1;
        AutoSkill2 = autoSkill2;
    }

    public BattleSkillSpec GetSkill(BattleSkillSlot slot)
    {
        return slot switch
        {
            BattleSkillSlot.BasicAttack => BasicAttack,
            BattleSkillSlot.Ultimate => Ultimate,
            BattleSkillSlot.AutoSkill1 => AutoSkill1,
            BattleSkillSlot.AutoSkill2 => AutoSkill2,
            _ => default,
        };
    }

    public static BattleSkillLoadout CreateDefault(BattleUnitSpec unitSpec)
        => Create(unitSpec, BattleSkillProfile.CreateDefault());

    public static BattleSkillLoadout Create(BattleUnitSpec unitSpec, BattleSkillProfile profile)
    {
        BattleSkillSpec basicAttack = BattleSkillSpec.FromConfig(BattleSkillSpec.CreateBasicAttackConfig(unitSpec, profile));
        BattleSkillSpec ultimate = BattleSkillSpec.FromConfig(BattleSkillSpec.CreateUltimateConfig(unitSpec, profile));
        BattleSkillSpec autoSkill1 = default;
        BattleSkillSpec autoSkill2 = default;
        for (int i = 0; i < profile.AutoSkillTemplates.Length; i++)
        {
            BattleSkillSpec skill = BattleSkillSpec.FromConfig(BattleSkillSpec.CreateAutoSkillConfig(unitSpec, profile.AutoSkillTemplates[i]));
            if (skill.Slot == BattleSkillSlot.AutoSkill1)
            {
                autoSkill1 = skill;
            }
            else if (skill.Slot == BattleSkillSlot.AutoSkill2)
            {
                autoSkill2 = skill;
            }
        }

        BattleConfigValidationResult basicResult = BattleConfigValidator.ValidateSkillSpec(basicAttack);
        if (!basicResult.IsValid)
        {
            throw new System.InvalidOperationException(basicResult.Message);
        }

        BattleConfigValidationResult ultimateResult = BattleConfigValidator.ValidateSkillSpec(ultimate);
        if (!ultimateResult.IsValid)
        {
            throw new System.InvalidOperationException(ultimateResult.Message);
        }

        if (autoSkill1.Type != BattleSkillType.None)
        {
            BattleConfigValidationResult autoResult = BattleConfigValidator.ValidateSkillSpec(autoSkill1);
            if (!autoResult.IsValid)
            {
                throw new System.InvalidOperationException(autoResult.Message);
            }
        }

        if (autoSkill2.Type != BattleSkillType.None)
        {
            BattleConfigValidationResult autoResult = BattleConfigValidator.ValidateSkillSpec(autoSkill2);
            if (!autoResult.IsValid)
            {
                throw new System.InvalidOperationException(autoResult.Message);
            }
        }

        return new BattleSkillLoadout(basicAttack, ultimate, autoSkill1, autoSkill2);
    }
}
