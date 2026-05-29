namespace LeanClr.Battle;

public readonly struct BattleCommand
{
    public readonly BattleCommandType Type;
    public readonly int TargetUnitId;

    public BattleCommand(BattleCommandType type, int targetUnitId)
    {
        Type = type;
        TargetUnitId = targetUnitId;
    }

    public static BattleCommand None => new(BattleCommandType.None, 0);
    public static BattleCommand CastUltimate(int targetUnitId = 0) => new(BattleCommandType.CastUltimate, targetUnitId);
}

