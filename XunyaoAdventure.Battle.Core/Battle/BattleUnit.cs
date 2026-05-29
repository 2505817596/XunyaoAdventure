using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleUnit
{
    private readonly System.Collections.Generic.List<BattleBuffInstance> _buffs = new();
    private readonly BattleAttributeSet _attributes = new();
    private readonly bool[] _passiveTriggerConsumed;

    public int Id { get; }
    public BattleTeam Team { get; }
    public BattleUnitSpec Spec { get; }
    public int SkillProfileId { get; }
    public BattleSkillProfile SkillProfile { get; }
    public BattleSkillLoadout Skills { get; }

    public FixVec2 Position { get; private set; }
    public FixVec2 Facing { get; private set; }
    public Fix32 Hp { get; private set; }
    public Fix32 MoveSpeed => _attributes.MoveSpeed;
    public Fix32 PhysicalAttack => _attributes.PhysicalAttack;
    public Fix32 AttackSpeed => _attributes.AttackSpeed;
    public Fix32 PhysicalArmor => _attributes.PhysicalArmor;
    public Fix32 MagicResist => _attributes.MagicResist;
    public Fix32 EnergyRegen => _attributes.EnergyRegen;
    public Fix32 MagicPower => _attributes.MagicPower;
    public Fix32 PhysicalCrit => _attributes.PhysicalCrit;
    public Fix32 HpRegen => _attributes.HpRegen;
    public Fix32 InterruptThreshold => _attributes.InterruptThreshold;
    public Fix32 Energy { get; private set; }
    public BattleUnitTag UnitTags { get; private set; }
    public bool IsAlive { get; private set; }
    public int CurrentTargetId { get; private set; }
    public Fix32 AttackCooldown { get; private set; }
    public Fix32 UltimateCooldown { get; private set; }
    public Fix32 AutoSkill1Cooldown { get; private set; }
    public Fix32 AutoSkill2Cooldown { get; private set; }
    public BattleUnitActionState ActionState { get; private set; }
    public BattleSkillRuntime? CurrentSkill { get; private set; }
    public BattleSkillType LastFinishedSkillType { get; private set; }

    internal BattleUnit(int id, BattleTeam team, BattleUnitSpec spec, int skillProfileId, FixVec2 position)
    {
        Id = id;
        Team = team;
        Spec = spec;
        SkillProfileId = skillProfileId;
        SkillProfile = BattleSkillProfiles.Resolve(skillProfileId);
        Skills = BattleSkillLoadout.Create(spec, SkillProfile);
        _passiveTriggerConsumed = new bool[SkillProfile.PassiveTriggers.Length];
        Position = position;
        Facing = FixVec2.Right;
        Hp = spec.MaxHp;
        _attributes.SetBase(BattleAttributeType.MoveSpeed, spec.MoveSpeed);
        _attributes.SetBase(BattleAttributeType.PhysicalAttack, spec.PhysicalAttack);
        _attributes.SetBase(BattleAttributeType.AttackSpeed, Fix32.One);
        _attributes.SetBase(BattleAttributeType.PhysicalArmor, spec.PhysicalArmor);
        _attributes.SetBase(BattleAttributeType.MagicResist, spec.MagicResist);
        _attributes.SetBase(BattleAttributeType.EnergyRegen, spec.EnergyRegenPerSecond);
        _attributes.SetBase(BattleAttributeType.MagicPower, spec.MagicPower);
        _attributes.SetBase(BattleAttributeType.PhysicalCrit, spec.PhysicalCrit);
        _attributes.SetBase(BattleAttributeType.HpRegen, spec.HpRegenPerSecond);
        _attributes.SetBase(BattleAttributeType.InterruptThreshold, spec.InterruptThreshold);
        Energy = Fix32.Zero;
        UnitTags = spec.UnitTags | SkillProfile.UnitTags;
        IsAlive = true;
        CurrentTargetId = 0;
        AttackCooldown = Fix32.Zero;
        UltimateCooldown = Fix32.Zero;
        AutoSkill1Cooldown = Fix32.Zero;
        AutoSkill2Cooldown = Fix32.Zero;
        ActionState = BattleUnitActionState.Idle;
        CurrentSkill = null;
        LastFinishedSkillType = BattleSkillType.None;
    }

    public void SetFacing(FixVec2 facing)
    {
        if (facing.SqrMagnitude > FixMath.Epsilon)
        {
            Facing = FixVec2.Normalize(facing);
        }
    }

    internal void SetTarget(int unitId)
    {
        CurrentTargetId = unitId;
    }

    internal void SetPosition(FixVec2 position)
    {
        Position = position;
    }

    internal void ReduceCooldown(Fix32 deltaTime)
    {
        UltimateCooldown = ReduceCooldownValue(UltimateCooldown, deltaTime);
        AutoSkill1Cooldown = ReduceCooldownValue(AutoSkill1Cooldown, deltaTime);
        AutoSkill2Cooldown = ReduceCooldownValue(AutoSkill2Cooldown, deltaTime);

        Energy += EnergyRegen * deltaTime;
        if (Energy > Fix32.One)
        {
            Energy = Fix32.One;
        }

        if (HpRegen > Fix32.Zero && Hp > Fix32.Zero && Hp < Spec.MaxHp)
        {
            Hp += HpRegen * deltaTime;
            if (Hp > Spec.MaxHp)
            {
                Hp = Spec.MaxHp;
            }
        }
    }

    internal bool IsStunned => IsAlive && HasBuff(BattleBuffType.Stun);
    internal bool IsBusy => CurrentSkill != null && !CurrentSkill.IsFinished;
    internal bool WasRevivedThisHit { get; private set; }
    internal Fix32 RevivedHpThisHit { get; private set; }
    internal bool IsControlImmune => IsAlive && HasBuff(BattleBuffType.ControlImmune);
    internal bool IsSilenceImmune => IsAlive && (HasBuff(BattleBuffType.SilenceImmune) || IsControlImmune);
    internal bool IsDisplaceImmune => IsAlive && (HasBuff(BattleBuffType.DisplaceImmune) || IsControlImmune);
    internal bool IsUninterruptible => IsAlive && HasBuff(BattleBuffType.Uninterruptible);
    internal bool IsDamageImmune => IsAlive && HasBuff(BattleBuffType.DamageImmune);
    internal bool IsPhysicalImmune => IsAlive && (IsDamageImmune || HasBuff(BattleBuffType.PhysicalImmune));
    internal bool IsMagicalImmune => IsAlive && (IsDamageImmune || HasBuff(BattleBuffType.MagicalImmune));
    internal bool IsPureImmune => IsAlive && (IsDamageImmune || HasBuff(BattleBuffType.PureImmune));
    internal bool IsControlled => IsAlive && (HasBuff(BattleBuffType.Stun) || HasBuff(BattleBuffType.Taunt) || HasBuff(BattleBuffType.Silence));
    public Fix32 HpRatio => Spec.MaxHp <= Fix32.Zero ? Fix32.Zero : Hp / Spec.MaxHp;
    internal bool HasUnitTag(BattleUnitTag tags) => tags == BattleUnitTag.None || (UnitTags & tags) == tags;
    internal void AddUnitTags(BattleUnitTag tags) => UnitTags |= tags;
}

public enum BattleUnitActionState
{
    Idle = 0,
    SkillWindup = 1,
    SkillRecover = 2,
    Dead = 3,
}

