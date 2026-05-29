using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleEffectResolver
{
    private readonly BattleWorld _world;

    public BattleEffectResolver(BattleWorld world)
    {
        _world = world;
    }

    public void ApplyBuff(BattleUnit source, BattleUnit target, BattleBuff buff)
    {
        if (!CanApplyBuff(target, buff.Type))
        {
            return;
        }

        if (target.ApplyBuff(buff))
        {
            _world.DispatchCombatEvent(new BattleCombatEvent(
                BattleCombatEventType.BuffApplied,
                source.Id,
                target.Id,
                buff.Duration,
                Fix32.Zero,
                source.Team,
                target.Team,
                buff.Type));
            _world.AddBattleEvent(BattleEvent.BuffApplied(target.Id, source.Id, buff, target.Team));
        }
    }

    public void ApplyDispel(BattleUnit source, BattleUnit target, int maxCount)
        => ApplyDispel(source, target, maxCount, BattleDispelMode.Any);

    public void ApplyDispel(BattleUnit source, BattleUnit target, int maxCount, BattleDispelMode mode)
    {
        int removed = target.DispelBuffs(maxCount, mode);
        if (removed > 0)
        {
            _world.DispatchCombatEvent(new BattleCombatEvent(
                BattleCombatEventType.BuffDispelled,
                source.Id,
                target.Id,
                Fix32.FromInt(removed),
                Fix32.Zero,
                source.Team,
                target.Team));
            _world.AddBattleEvent(BattleEvent.BuffDispelled(target.Id, source.Id, Fix32.FromInt(removed), target.Team));
        }
    }

    public void ApplyHeal(BattleUnit source, BattleUnit target, Fix32 heal)
    {
        Fix32 before = target.Hp;
        target.ReceiveHeal(heal, source);
        Fix32 actual = target.Hp - before;
        if (actual <= Fix32.Zero)
        {
            return;
        }

        _world.DispatchCombatEvent(new BattleCombatEvent(
            BattleCombatEventType.Healed,
            source.Id,
            target.Id,
            actual,
            target.Hp,
            source.Team,
            target.Team));
        _world.AddBattleEvent(BattleEvent.UnitHealed(target.Id, source.Id, actual, target.Hp, target.Team));
        _world.TryTriggerHealGrantedPassives(source, target);
        _world.TryTriggerHealReceivedPassives(target, source);
    }

    public void ApplyEnergy(BattleUnit source, BattleUnit target, Fix32 amount)
    {
        Fix32 before = target.Energy;
        target.ModifyEnergy(amount);
        Fix32 actual = target.Energy - before;
        if (actual == Fix32.Zero)
        {
            return;
        }

        _world.DispatchCombatEvent(new BattleCombatEvent(
            BattleCombatEventType.Healed,
            source.Id,
            target.Id,
            actual,
            target.Energy,
            source.Team,
            target.Team));
        _world.AddBattleEvent(BattleEvent.UnitEnergyChanged(target.Id, source.Id, actual, target.Energy, target.Team));
        _world.TryTriggerHealGrantedPassives(source, target);
        _world.TryTriggerHealReceivedPassives(target, source);
    }

    public void ApplyDisplace(BattleUnit source, BattleUnit target, Fix32 distance)
    {
        if (!source.IsAlive || !target.IsAlive || distance == Fix32.Zero || target.IsDisplaceImmune)
        {
            return;
        }

        FixVec2 delta = target.Position - source.Position;
        Fix32 magnitude = delta.Magnitude;
        if (magnitude <= FixMath.Epsilon)
        {
            delta = source.Team == target.Team ? FixVec2.Right : FixVec2.Left;
            magnitude = Fix32.One;
        }

        FixVec2 direction = delta / magnitude;
        FixVec2 newPosition = target.Position + direction * distance;
        target.SetPosition(newPosition);
        target.SetFacing(direction);
        if (!target.IsUninterruptible)
        {
            target.CancelAction();
        }

        _world.AddBattleEvent(BattleEvent.UnitMoved(target.Id, target.Position, target.Team));
    }

    public void ApplyInterrupt(BattleUnit source, BattleUnit target)
    {
        if (!source.IsAlive || !target.IsAlive || target.IsUninterruptible || target.IsControlImmune)
        {
            return;
        }

        if (target.ActionState != BattleUnitActionState.SkillWindup && target.ActionState != BattleUnitActionState.SkillRecover)
        {
            return;
        }

        if (target.InterruptThreshold > Fix32.Zero && source.PhysicalAttack < target.InterruptThreshold)
        {
            return;
        }

        target.CancelAction();
    }

    public void ApplyPeriodicHeal(BattleUnit source, BattleUnit target, Fix32 heal, BattleBuffType buffType)
    {
        if (!source.IsAlive || !target.IsAlive || heal <= Fix32.Zero)
        {
            return;
        }

        Fix32 before = target.Hp;
        target.ReceiveHeal(heal, source);
        Fix32 actual = target.Hp - before;
        if (actual <= Fix32.Zero)
        {
            return;
        }

        _world.DispatchCombatEvent(new BattleCombatEvent(
            BattleCombatEventType.Healed,
            source.Id,
            target.Id,
            actual,
            target.Hp,
            source.Team,
            target.Team,
            buffType));
        _world.AddBattleEvent(BattleEvent.BuffTriggered(target.Id, source.Id, buffType, actual, target.Team));
        _world.AddBattleEvent(BattleEvent.UnitHealed(target.Id, source.Id, actual, target.Hp, target.Team));
        _world.TryTriggerHealGrantedPassives(source, target);
        _world.TryTriggerHealReceivedPassives(target, source);
    }

    private static bool CanApplyBuff(BattleUnit target, BattleBuffType buffType)
    {
        if (buffType == BattleBuffType.ControlImmune
            || buffType == BattleBuffType.SilenceImmune
            || buffType == BattleBuffType.DisplaceImmune
            || buffType == BattleBuffType.Uninterruptible)
        {
            return true;
        }

        if (target.IsControlImmune && IsControlBuff(buffType))
        {
            return false;
        }

        if (target.IsSilenceImmune && buffType == BattleBuffType.Silence)
        {
            return false;
        }

        return true;
    }

    private static bool IsControlBuff(BattleBuffType buffType)
        => buffType == BattleBuffType.Stun || buffType == BattleBuffType.Taunt || buffType == BattleBuffType.Silence;
}
