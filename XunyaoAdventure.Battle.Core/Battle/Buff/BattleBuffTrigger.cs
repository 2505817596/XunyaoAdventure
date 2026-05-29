using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal readonly struct BattleBuffTrigger
{
    public BattleBuffTrigger(int unitId, BattleTeam team, BattleBuffType buffType, Fix32 amount, int value = 0)
    {
        UnitId = unitId;
        Team = team;
        BuffType = buffType;
        Amount = amount;
        Value = value;
    }

    public readonly int UnitId;
    public readonly BattleTeam Team;
    public readonly BattleBuffType BuffType;
    public readonly Fix32 Amount;
    public readonly int Value;
}
