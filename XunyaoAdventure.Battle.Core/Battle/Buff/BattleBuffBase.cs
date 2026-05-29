using LeanClr.Mathematics;

namespace LeanClr.Battle;

public enum BattleBuffType
{
    None = 0,
    Stun = 1,
    PhysicalArmorUp = 2,
    Shield = 3,
    Thorns = 4,
    LifeSteal = 5,
    Rage = 6,
    SecondWind = 7,
    DeathBurst = 8,
    Slow = 9,
    Silence = 10,
    PhysicalAttackUp = 11,
    AttackSpeedUp = 12,
    EnergyRegenUp = 13,
    MagicResistUp = 15,
    PhysicalAttackDown = 16,
    AttackSpeedDown = 17,
    EnergyRegenDown = 18,
    PhysicalArmorDown = 19,
    MagicResistDown = 20,
    Taunt = 21,
    Revive = 22,
    SummonLife = 23,
    DamageOverTime = 24,
    HealOverTime = 25,
    ControlImmune = 26,
    SilenceImmune = 27,
    DisplaceImmune = 28,
    Uninterruptible = 29,
    DamageImmune = 30,
    PhysicalImmune = 31,
    MagicalImmune = 32,
    PureImmune = 33,
    HealingAmplify = 34,
    HealingReduce = 35,
    DamageTakenAmplify = 36,
    DamageTakenReduce = 37,
    PhysicalDamageTakenAmplify = 38,
    PhysicalDamageTakenReduce = 39,
    MagicalDamageTakenAmplify = 40,
    MagicalDamageTakenReduce = 41,
    PureDamageTakenAmplify = 42,
    PureDamageTakenReduce = 43,
}

public abstract class BattleBuff
{
    protected BattleBuff(BattleBuffType type, Fix32 duration)
    {
        Type = type;
        Duration = duration;
    }

    public BattleBuffType Type { get; }
    public Fix32 Duration { get; }
    public virtual Fix32 Magnitude => Fix32.Zero;
    public virtual bool BlocksControl => false;
    public virtual int BeforeReceiveDamagePriority => 0;

    public virtual void OnApply(BattleUnit unit) { }
    public virtual void OnTick(BattleUnit unit, Fix32 deltaTime) { }
    public virtual void OnTick(BattleWorld world, BattleUnit unit, Fix32 deltaTime) => OnTick(unit, deltaTime);
    public virtual Fix32 OnBeforeDealDamage(BattleUnit unit, BattleUnit target, Fix32 damage) => damage;
    public virtual Fix32 OnBeforeReceiveDamage(BattleUnit unit, BattleUnit source, Fix32 damage, BattleDamageType damageType) => damage;
    public virtual Fix32 OnBeforeReceiveHealing(BattleUnit unit, BattleUnit source, Fix32 healing) => healing;
    public virtual Fix32 OnAfterDealDamage(BattleUnit unit, BattleUnit target, Fix32 actualDamage) => Fix32.Zero;
    public virtual Fix32 OnAfterReceiveDamage(BattleUnit unit, BattleUnit source, Fix32 actualDamage) => Fix32.Zero;
    public virtual Fix32 OnCombatEvent(BattleUnit unit, in BattleCombatEvent combatEvent) => Fix32.Zero;
    public virtual void OnExpire(BattleUnit unit) { }
    public virtual void OnRemove(BattleUnit unit) { }
    public virtual bool IsConsumed => false;
    public virtual bool CanStackWith(BattleBuff other) => Type == other.Type;
}
