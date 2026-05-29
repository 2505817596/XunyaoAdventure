using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleBuffInstance
{
    public BattleBuffInstance(BattleBuff buff, Fix32 remainingTime)
    {
        Buff = buff;
        RemainingTime = remainingTime;
    }

    public BattleBuff Buff { get; }
    public Fix32 RemainingTime { get; set; }
}
