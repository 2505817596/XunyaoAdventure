using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class BattleDamageImmunityBuff : BattleBuff
{
    public BattleDamageImmunityBuff(BattleBuffType type, Fix32 duration) : base(type, duration)
    {
    }

    public override int BeforeReceiveDamagePriority => 100;

    public override Fix32 OnBeforeReceiveDamage(BattleUnit unit, BattleUnit source, Fix32 damage, BattleDamageType damageType)
        => IsImmuneTo(damageType) ? Fix32.Zero : damage;

    private bool IsImmuneTo(BattleDamageType damageType)
    {
        return Type switch
        {
            BattleBuffType.DamageImmune => damageType == BattleDamageType.Physical
                || damageType == BattleDamageType.Magical
                || damageType == BattleDamageType.Pure,
            BattleBuffType.PhysicalImmune => damageType == BattleDamageType.Physical,
            BattleBuffType.MagicalImmune => damageType == BattleDamageType.Magical,
            BattleBuffType.PureImmune => damageType == BattleDamageType.Pure,
            _ => false,
        };
    }
}
