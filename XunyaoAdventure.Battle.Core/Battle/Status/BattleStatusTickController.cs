using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleStatusTickController
{
    private static readonly Fix32 AuraTickInterval = Fix32.FromInt(1);

    private readonly BattleWorld _world;
    private readonly BattlePassiveTriggerResolver _passiveTriggerResolver;
    private Fix32 _auraTickAccumulator;

    public BattleStatusTickController(BattleWorld world, BattlePassiveTriggerResolver passiveTriggerResolver)
    {
        _world = world;
        _passiveTriggerResolver = passiveTriggerResolver;
    }

    public void TickUnitBuffs(BattleUnit unit, Fix32 deltaTime)
    {
        if (unit.TickBuffs(_world, deltaTime, out BattleBuffType expiredBuff))
        {
            _world.AddBattleEvent(BattleEvent.BuffExpired(unit.Id, expiredBuff, unit.Team));
            if (expiredBuff == BattleBuffType.SummonLife)
            {
                unit.Despawn();
                _world.MarkUnitForRemoval(unit.Id);
                return;
            }
        }

        while (unit.TryRemoveConsumedBuff(out BattleBuffType consumedBuff))
        {
            _world.AddBattleEvent(BattleEvent.BuffExpired(unit.Id, consumedBuff, unit.Team));
        }
    }

    public void TickAuraPassives(Fix32 deltaTime)
    {
        _auraTickAccumulator += deltaTime;
        if (_auraTickAccumulator < AuraTickInterval)
        {
            return;
        }

        _auraTickAccumulator -= AuraTickInterval;
        while (_auraTickAccumulator >= AuraTickInterval)
        {
            _auraTickAccumulator -= AuraTickInterval;
        }

        _passiveTriggerResolver.TriggerAuraTickPassives();
    }
}
