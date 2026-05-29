namespace LeanClr.Battle;

public enum BattleBuffStackMode
{
    Unique = 0,
    Stack = 1,
}

public enum BattleBuffRefreshMode
{
    None = 0,
    RefreshDuration = 1,
    ReplaceIfStronger = 2,
}

public readonly struct BattleBuffProfile
{
    public readonly BattleBuffType Type;
    public readonly BattleBuffStackMode StackMode;
    public readonly BattleBuffRefreshMode RefreshMode;
    public readonly int MaxStacks;
    public readonly bool Dispellable;
    public readonly bool IsDebuff;

    public BattleBuffProfile(
        BattleBuffType type,
        BattleBuffStackMode stackMode,
        BattleBuffRefreshMode refreshMode,
        int maxStacks,
        bool dispellable,
        bool isDebuff = false)
    {
        Type = type;
        StackMode = stackMode;
        RefreshMode = refreshMode;
        MaxStacks = maxStacks;
        Dispellable = dispellable;
        IsDebuff = isDebuff;
    }
}

public static class BattleBuffProfiles
{
    private static readonly BattleBuffProfile[] Profiles =
    {
        new(BattleBuffType.None, BattleBuffStackMode.Unique, BattleBuffRefreshMode.None, 0, false),
        new(BattleBuffType.Stun, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true, true),
        new(BattleBuffType.PhysicalArmorUp, BattleBuffStackMode.Stack, BattleBuffRefreshMode.None, 5, true),
        new(BattleBuffType.Shield, BattleBuffStackMode.Stack, BattleBuffRefreshMode.None, 3, true),
        new(BattleBuffType.Thorns, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.LifeSteal, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.Rage, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.SecondWind, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.DeathBurst, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.Slow, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true, true),
        new(BattleBuffType.Silence, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true, true),
        new(BattleBuffType.PhysicalAttackUp, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 5, true),
        new(BattleBuffType.AttackSpeedUp, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 5, true),
        new(BattleBuffType.EnergyRegenUp, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 5, true),
        new(BattleBuffType.MagicResistUp, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 5, true),
        new(BattleBuffType.PhysicalAttackDown, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 5, true, true),
        new(BattleBuffType.AttackSpeedDown, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 5, true, true),
        new(BattleBuffType.EnergyRegenDown, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 5, true, true),
        new(BattleBuffType.PhysicalArmorDown, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 5, true, true),
        new(BattleBuffType.MagicResistDown, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 5, true, true),
        new(BattleBuffType.Taunt, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true, true),
        new(BattleBuffType.Revive, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, false),
        new(BattleBuffType.SummonLife, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, false),
        new(BattleBuffType.DamageOverTime, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 3, true, true),
        new(BattleBuffType.HealOverTime, BattleBuffStackMode.Stack, BattleBuffRefreshMode.RefreshDuration, 3, true),
        new(BattleBuffType.ControlImmune, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.SilenceImmune, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.DisplaceImmune, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.Uninterruptible, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.DamageImmune, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.PhysicalImmune, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.MagicalImmune, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.PureImmune, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.HealingAmplify, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.HealingReduce, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true, true),
        new(BattleBuffType.DamageTakenAmplify, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true, true),
        new(BattleBuffType.DamageTakenReduce, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.PhysicalDamageTakenAmplify, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true, true),
        new(BattleBuffType.PhysicalDamageTakenReduce, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.MagicalDamageTakenAmplify, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true, true),
        new(BattleBuffType.MagicalDamageTakenReduce, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
        new(BattleBuffType.PureDamageTakenAmplify, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true, true),
        new(BattleBuffType.PureDamageTakenReduce, BattleBuffStackMode.Unique, BattleBuffRefreshMode.RefreshDuration, 1, true),
    };

    public static BattleBuffProfile Resolve(BattleBuffType type)
    {
        for (int i = 0; i < Profiles.Length; i++)
        {
            if (Profiles[i].Type == type)
            {
                return Profiles[i];
            }
        }

        return Profiles[0];
    }

    public static bool CanDispel(BattleBuffType type) => Resolve(type).Dispellable;
    public static bool IsDebuff(BattleBuffType type) => Resolve(type).IsDebuff;
    public static bool IsBuff(BattleBuffType type)
    {
        BattleBuffProfile profile = Resolve(type);
        return profile.Type != BattleBuffType.None && !profile.IsDebuff;
    }
}
