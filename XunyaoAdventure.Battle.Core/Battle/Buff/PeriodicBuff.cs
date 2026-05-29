using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed class PeriodicBuff : BattleBuff
{
    private readonly int _sourceUnitId;
    private readonly Fix32 _tickInterval;
    private Fix32 _remainingToNextTick;

    public PeriodicBuff(BattleBuffType type, Fix32 duration, Fix32 amountPerTick, Fix32 tickInterval, int sourceUnitId)
        : base(type, duration)
    {
        AmountPerTick = amountPerTick;
        _tickInterval = tickInterval > Fix32.Zero ? tickInterval : Fix32.One;
        _remainingToNextTick = _tickInterval;
        _sourceUnitId = sourceUnitId;
    }

    public Fix32 AmountPerTick { get; }
    public override Fix32 Magnitude => AmountPerTick;

    public override void OnTick(BattleWorld world, BattleUnit unit, Fix32 deltaTime)
    {
        if (deltaTime <= Fix32.Zero || AmountPerTick <= Fix32.Zero)
        {
            return;
        }

        _remainingToNextTick -= deltaTime;
        while (_remainingToNextTick <= Fix32.Zero && unit.IsAlive)
        {
            BattleUnit source = world.ResolveEffectSource(_sourceUnitId, unit);
            if (Type == BattleBuffType.DamageOverTime)
            {
                world.ApplyPeriodicDamage(source, unit, AmountPerTick, Type);
            }
            else if (Type == BattleBuffType.HealOverTime)
            {
                world.ApplyPeriodicHeal(source, unit, AmountPerTick, Type);
            }

            _remainingToNextTick += _tickInterval;
        }
    }
}
