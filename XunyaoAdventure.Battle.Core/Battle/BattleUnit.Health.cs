using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleUnit
{
    internal void ReceiveHeal(Fix32 amount)
        => ReceiveHeal(amount, null);

    internal void ReceiveHeal(Fix32 amount, BattleUnit? source)
    {
        if (!IsAlive || amount <= Fix32.Zero)
        {
            return;
        }

        Fix32 healing = amount;
        for (int i = 0; i < _buffs.Count; i++)
        {
            BattleBuff buff = _buffs[i].Buff;
            healing = buff.OnBeforeReceiveHealing(this, source ?? this, healing);
            if (healing < Fix32.Zero)
            {
                healing = Fix32.Zero;
            }
        }

        Hp += healing;
        if (Hp > Spec.MaxHp)
        {
            Hp = Spec.MaxHp;
        }
    }

    internal Fix32 ModifyOutgoingDamage(BattleUnit target, Fix32 damage)
    {
        Fix32 result = damage;
        for (int i = 0; i < _buffs.Count; i++)
        {
            result = _buffs[i].Buff.OnBeforeDealDamage(this, target, result);
            if (result < Fix32.Zero)
            {
                result = Fix32.Zero;
            }
        }

        return result;
    }

    internal Fix32 ModifyIncomingDamage(BattleUnit source, Fix32 damage, BattleDamageType damageType)
    {
        Fix32 result = damage;
        ApplyIncomingDamageModifiers(source, damageType, 1, ref result);
        ApplyIncomingDamageModifiers(source, damageType, 0, ref result);
        ApplyIncomingDamageModifiers(source, damageType, -1, ref result);

        return result;
    }

    private void ApplyIncomingDamageModifiers(BattleUnit source, BattleDamageType damageType, int priorityBucket, ref Fix32 result)
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            BattleBuff buff = _buffs[i].Buff;
            int priority = buff.BeforeReceiveDamagePriority;
            if ((priorityBucket > 0 && priority <= 0)
                || (priorityBucket == 0 && priority != 0)
                || (priorityBucket < 0 && priority >= 0))
            {
                continue;
            }

            result = buff.OnBeforeReceiveDamage(this, source, result, damageType);
            if (result < Fix32.Zero)
            {
                result = Fix32.Zero;
            }
        }
    }

    internal Fix32 NotifyAfterDealDamage(BattleUnit target, Fix32 actualDamage)
    {
        Fix32 total = Fix32.Zero;
        for (int i = 0; i < _buffs.Count; i++)
        {
            Fix32 triggered = _buffs[i].Buff.OnAfterDealDamage(this, target, actualDamage);
            if (triggered > Fix32.Zero)
            {
                total += triggered;
            }
        }

        return total;
    }

    internal Fix32 NotifyAfterReceiveDamage(BattleUnit source, Fix32 actualDamage)
    {
        Fix32 total = Fix32.Zero;
        for (int i = 0; i < _buffs.Count; i++)
        {
            Fix32 triggered = _buffs[i].Buff.OnAfterReceiveDamage(this, source, actualDamage);
            if (triggered > Fix32.Zero)
            {
                total += triggered;
            }
        }

        return total;
    }

    internal void CancelAction()
    {
        if (!IsAlive)
        {
            return;
        }

        ActionState = BattleUnitActionState.Idle;
        CurrentSkill = null;
        LastFinishedSkillType = BattleSkillType.None;
    }

    internal void ClearLastFinishedSkill()
    {
        LastFinishedSkillType = BattleSkillType.None;
    }

    internal void Despawn()
    {
        if (!IsAlive)
        {
            return;
        }

        IsAlive = false;
        Hp = Fix32.Zero;
        Energy = Fix32.Zero;
        CurrentTargetId = 0;
        AttackCooldown = Fix32.Zero;
        UltimateCooldown = Fix32.Zero;
        AutoSkill1Cooldown = Fix32.Zero;
        AutoSkill2Cooldown = Fix32.Zero;
        CurrentSkill = null;
        LastFinishedSkillType = BattleSkillType.None;
        ActionState = BattleUnitActionState.Dead;
        RemoveAllBuffs();
    }

    internal void MoveTowards(FixVec2 target, Fix32 deltaTime, out bool moved)
    {
        moved = false;
        FixVec2 delta = target - Position;
        Fix32 distance = delta.Magnitude;
        if (distance <= FixMath.Epsilon)
        {
            return;
        }

        Fix32 step = MoveSpeed * deltaTime;
        if (step >= distance)
        {
            Position = target;
            moved = true;
            SetFacing(delta);
            return;
        }

        FixVec2 direction = delta / distance;
        Position += direction * step;
        SetFacing(direction);
        moved = true;
    }

    internal void MoveAxisFirstTowards(FixVec2 target, Fix32 range, Fix32 deltaTime, out bool moved)
    {
        moved = false;
        Fix32 step = MoveSpeed * deltaTime;
        if (step <= Fix32.Zero)
        {
            return;
        }

        Fix32 deltaX = target.X - Position.X;
        Fix32 absDeltaX = Fix32.Abs(deltaX);
        Fix32 stopRange = range > Fix32.Zero ? range : Fix32.Zero;
        if (absDeltaX > stopRange)
        {
            Fix32 desiredX = target.X - Sign(deltaX) * stopRange;
            MoveAxisX(desiredX, step, out moved);
            return;
        }

        Fix32 deltaY = target.Y - Position.Y;
        Fix32 absDeltaY = Fix32.Abs(deltaY);
        Fix32 allowedY = ResolveAllowedY(stopRange, absDeltaX);
        if (absDeltaY > allowedY)
        {
            Fix32 desiredY = target.Y - Sign(deltaY) * allowedY;
            MoveAxisY(desiredY, step, out moved);
        }
    }

    private void MoveAxisX(Fix32 targetX, Fix32 step, out bool moved)
    {
        moved = false;
        Fix32 delta = targetX - Position.X;
        Fix32 distance = Fix32.Abs(delta);
        if (distance <= FixMath.Epsilon)
        {
            return;
        }

        Fix32 move = step >= distance ? distance : step;
        Fix32 direction = Sign(delta);
        Position = new FixVec2(Position.X + direction * move, Position.Y);
        SetFacing(new FixVec2(direction, Fix32.Zero));
        moved = true;
    }

    private void MoveAxisY(Fix32 targetY, Fix32 step, out bool moved)
    {
        moved = false;
        Fix32 delta = targetY - Position.Y;
        Fix32 distance = Fix32.Abs(delta);
        if (distance <= FixMath.Epsilon)
        {
            return;
        }

        Fix32 move = step >= distance ? distance : step;
        Fix32 direction = Sign(delta);
        Position = new FixVec2(Position.X, Position.Y + direction * move);
        SetFacing(new FixVec2(Fix32.Zero, direction));
        moved = true;
    }

    private static Fix32 ResolveAllowedY(Fix32 range, Fix32 absDeltaX)
    {
        if (range <= Fix32.Zero || absDeltaX >= range)
        {
            return Fix32.Zero;
        }

        return FixMath.Sqrt(range * range - absDeltaX * absDeltaX);
    }

    private static Fix32 Sign(Fix32 value)
        => value >= Fix32.Zero ? Fix32.One : -Fix32.One;

    internal bool CanCastUltimate()
    {
        BattleSkillSpec ultimate = Skills.GetSkill(BattleSkillSlot.Ultimate);
        return IsAlive && !IsStunned && (IsSilenceImmune || !HasBuff(BattleBuffType.Silence)) && UltimateCooldown <= Fix32.Zero && Energy >= ultimate.EnergyCost;
    }

    internal bool CanCastAutoSkill(BattleSkillSlot slot)
    {
        BattleSkillSpec skill = Skills.GetSkill(slot);
        if (skill.Type == BattleSkillType.None || skill.Cooldown <= Fix32.Zero)
        {
            return false;
        }

        Fix32 cooldown = slot == BattleSkillSlot.AutoSkill1 ? AutoSkill1Cooldown : AutoSkill2Cooldown;
        return IsAlive && !IsStunned && (IsSilenceImmune || !HasBuff(BattleBuffType.Silence)) && cooldown <= Fix32.Zero;
    }

    internal void ConsumeSkillCost(BattleSkillSpec skill)
    {
        Energy -= skill.EnergyCost;
        if (Energy < Fix32.Zero)
        {
            Energy = Fix32.Zero;
        }

        if (skill.Slot == BattleSkillSlot.Ultimate)
        {
            UltimateCooldown = skill.Cooldown;
        }
        else if (skill.Slot == BattleSkillSlot.AutoSkill1)
        {
            AutoSkill1Cooldown = skill.Cooldown;
        }
        else if (skill.Slot == BattleSkillSlot.AutoSkill2)
        {
            AutoSkill2Cooldown = skill.Cooldown;
        }
    }

    internal Fix32 ReceiveDamage(Fix32 amount)
        => ReceiveDamage(amount, BattleDamageType.Physical);

    internal Fix32 ReceiveDamage(Fix32 amount, BattleDamageType damageType)
    {
        WasRevivedThisHit = false;
        RevivedHpThisHit = Fix32.Zero;
        if (!IsAlive)
        {
            return Fix32.Zero;
        }

        Fix32 actualDamage = amount > Fix32.Zero ? amount : Fix32.Zero;

        Hp -= actualDamage;
        if (Hp <= Fix32.Zero)
        {
            if (TryConsumeReviveBuff(out Fix32 reviveHp))
            {
                Hp = reviveHp;
                IsAlive = true;
                ActionState = BattleUnitActionState.Idle;
                CurrentSkill = null;
                LastFinishedSkillType = BattleSkillType.None;
                RevivedHpThisHit = Hp;
                WasRevivedThisHit = true;
                return actualDamage;
            }

            Hp = Fix32.Zero;
            IsAlive = false;
            RemoveAllBuffs();
            CurrentSkill = null;
            LastFinishedSkillType = BattleSkillType.None;
            ActionState = BattleUnitActionState.Dead;
        }

        return actualDamage;
    }

    internal Fix32 ReceiveDamageFromBuff(Fix32 amount)
        => ReceiveDamageFromBuff(amount, BattleDamageType.Magical);

    internal Fix32 ReceiveDamageFromBuff(Fix32 amount, BattleDamageType damageType)
    {
        WasRevivedThisHit = false;
        RevivedHpThisHit = Fix32.Zero;
        if (!IsAlive)
        {
            return Fix32.Zero;
        }

        Fix32 actualDamage = amount > Fix32.Zero ? amount : Fix32.Zero;

        Hp -= actualDamage;
        if (Hp <= Fix32.Zero)
        {
            if (TryConsumeReviveBuff(out Fix32 reviveHp))
            {
                Hp = reviveHp;
                IsAlive = true;
                ActionState = BattleUnitActionState.Idle;
                CurrentSkill = null;
                LastFinishedSkillType = BattleSkillType.None;
                RevivedHpThisHit = Hp;
                WasRevivedThisHit = true;
                return actualDamage;
            }

            Hp = Fix32.Zero;
            IsAlive = false;
            RemoveAllBuffs();
            CurrentSkill = null;
            LastFinishedSkillType = BattleSkillType.None;
            ActionState = BattleUnitActionState.Dead;
        }

        return actualDamage;
    }

    internal Fix32 ResolveDamageReductionAttribute(BattleDamageType damageType)
        => damageType switch
        {
            BattleDamageType.Pure => Fix32.Zero,
            BattleDamageType.Magical => MagicResist,
            _ => PhysicalArmor,
        };
}

