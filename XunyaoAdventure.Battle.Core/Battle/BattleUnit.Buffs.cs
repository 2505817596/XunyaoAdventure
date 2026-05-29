using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleUnit
{
    internal bool TickBuffs(Fix32 deltaTime, out BattleBuffType expiredBuffType)
        => TickBuffs(null, deltaTime, out expiredBuffType);

    internal bool TickBuffs(BattleWorld? world, Fix32 deltaTime, out BattleBuffType expiredBuffType)
    {
        expiredBuffType = BattleBuffType.None;
        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            BattleBuffInstance buff = _buffs[i];
            if (world == null)
            {
                buff.Buff.OnTick(this, deltaTime);
            }
            else
            {
                buff.Buff.OnTick(world, this, deltaTime);
            }

            if (!IsAlive)
            {
                return false;
            }

            buff.RemainingTime -= deltaTime;
            if (buff.RemainingTime < Fix32.Zero)
            {
                buff.RemainingTime = Fix32.Zero;
            }

            _buffs[i] = buff;
            if (buff.RemainingTime == Fix32.Zero)
            {
                expiredBuffType = buff.Buff.Type;
                buff.Buff.OnExpire(this);
                _buffs.RemoveAt(i);
                continue;
            }

            if (buff.Buff.IsConsumed)
            {
                expiredBuffType = buff.Buff.Type;
                buff.Buff.OnRemove(this);
                _buffs.RemoveAt(i);
            }
        }

        return expiredBuffType != BattleBuffType.None;
    }

    internal void NotifyCombatEvent(in BattleCombatEvent combatEvent, System.Collections.Generic.List<BattleBuffTrigger> triggers)
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            Fix32 amount = _buffs[i].Buff.OnCombatEvent(this, combatEvent);
            if (amount <= Fix32.Zero)
            {
                continue;
            }

            triggers.Add(new BattleBuffTrigger(Id, Team, _buffs[i].Buff.Type, amount));
        }
    }

    internal bool TryRemoveConsumedBuff(out BattleBuffType removedBuffType)
    {
        removedBuffType = BattleBuffType.None;
        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            BattleBuffInstance buff = _buffs[i];
            if (!buff.Buff.IsConsumed)
            {
                continue;
            }

            removedBuffType = buff.Buff.Type;
            buff.Buff.OnRemove(this);
            _buffs.RemoveAt(i);
            return true;
        }

        return false;
    }

    internal bool ApplyBuff(BattleBuff buff)
    {
        if (!IsAlive)
        {
            return false;
        }

        BattleBuffProfile profile = BattleBuffProfiles.Resolve(buff.Type);
        int index = FindBuffIndex(buff.Type);
        if (index >= 0)
        {
            BattleBuffInstance existing = _buffs[index];
            if (profile.RefreshMode == BattleBuffRefreshMode.RefreshDuration)
            {
                existing.Buff.OnRemove(this);
                buff.OnApply(this);
                _buffs[index] = new BattleBuffInstance(buff, buff.Duration);
            }
            else if (profile.RefreshMode == BattleBuffRefreshMode.ReplaceIfStronger)
            {
                if (buff.Magnitude <= existing.Buff.Magnitude)
                {
                    return false;
                }

                existing.Buff.OnRemove(this);
                _buffs[index] = new BattleBuffInstance(buff, buff.Duration);
                buff.OnApply(this);
            }
            else if (profile.StackMode == BattleBuffStackMode.Stack)
            {
                int stackCount = CountBuffStacks(buff.Type);
                if (stackCount >= profile.MaxStacks)
                {
                    return false;
                }

                _buffs.Add(new BattleBuffInstance(buff, buff.Duration));
                buff.OnApply(this);
            }
            else
            {
                return false;
            }

            if (buff.BlocksControl && !IsUninterruptible)
            {
                CancelAction();
            }

            return true;
        }

        if (profile.StackMode == BattleBuffStackMode.Stack && _buffs.Count >= profile.MaxStacks)
        {
            return false;
        }

        _buffs.Add(new BattleBuffInstance(buff, buff.Duration));
        buff.OnApply(this);
        if (buff.BlocksControl && !IsUninterruptible)
        {
            CancelAction();
        }
        return true;
    }

    internal bool HasBuff(BattleBuffType type)
    {
        return FindBuffIndex(type) >= 0;
    }

    internal bool IsPassiveTriggerConsumed(int index)
        => (uint)index >= (uint)_passiveTriggerConsumed.Length || _passiveTriggerConsumed[index];

    internal void ConsumePassiveTrigger(int index)
    {
        if ((uint)index < (uint)_passiveTriggerConsumed.Length)
        {
            _passiveTriggerConsumed[index] = true;
        }
    }

    internal bool IsTaunted => IsAlive && HasBuff(BattleBuffType.Taunt);

    internal int DispelBuffs(int maxCount)
        => DispelBuffs(maxCount, BattleDispelMode.Any);

    internal int DispelBuffs(int maxCount, BattleDispelMode mode)
    {
        if (maxCount <= 0)
        {
            return 0;
        }

        int removedCount = 0;
        for (int i = _buffs.Count - 1; i >= 0 && maxCount > 0; i--)
        {
            BattleBuffInstance buff = _buffs[i];
            if (!BattleBuffProfiles.CanDispel(buff.Buff.Type))
            {
                continue;
            }

            if (!MatchesDispelMode(buff.Buff.Type, mode))
            {
                continue;
            }

            buff.Buff.OnRemove(this);
            _buffs.RemoveAt(i);
            maxCount--;
            removedCount++;
        }

        return removedCount;
    }

    private static bool MatchesDispelMode(BattleBuffType buffType, BattleDispelMode mode)
    {
        return mode switch
        {
            BattleDispelMode.BuffOnly => BattleBuffProfiles.IsBuff(buffType),
            BattleDispelMode.DebuffOnly => BattleBuffProfiles.IsDebuff(buffType),
            _ => true,
        };
    }
}

