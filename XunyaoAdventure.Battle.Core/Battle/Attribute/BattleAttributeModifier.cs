using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly struct BattleAttributeModifier
{
    public BattleAttributeModifier(int id, BattleAttributeType type, BattleAttributeModifierOp op, Fix32 value)
    {
        Id = id;
        Type = type;
        Op = op;
        Value = value;
    }

    public readonly int Id;
    public readonly BattleAttributeType Type;
    public readonly BattleAttributeModifierOp Op;
    public readonly Fix32 Value;
}
