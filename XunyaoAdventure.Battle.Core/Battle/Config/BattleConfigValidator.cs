namespace LeanClr.Battle;

public static partial class BattleConfigValidator
{
    public static BattleConfigValidationResult ValidateAll()
    {
        BattleConfigValidationResult result = BattleSkillProfiles.Validate();
        if (!result.IsValid)
        {
            return result;
        }

        return BattleConfigValidationResult.Success();
    }
}

