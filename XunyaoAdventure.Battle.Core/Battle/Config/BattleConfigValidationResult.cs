namespace LeanClr.Battle;

public sealed class BattleConfigValidationResult
{
    public BattleConfigValidationResult(bool isValid, string message)
    {
        IsValid = isValid;
        Message = message ?? string.Empty;
    }

    public bool IsValid { get; }
    public string Message { get; }

    public static BattleConfigValidationResult Success()
        => new(true, string.Empty);

    public static BattleConfigValidationResult Fail(string message)
        => new(false, message);
}
