using LeanClr.Battle;
using LeanClr.Mathematics;

namespace XunyaoAdventure.Battle;

internal enum HeroRank
{
    White = 0,
    Green = 1,
    Blue = 2,
    Purple = 3,
    Red = 4,
}

internal readonly record struct HeroAttributeBundle(
    double Strength = 0,
    double Intelligence = 0,
    double Agility = 0,
    double MaxHp = 0,
    double PhysicalAttack = 0,
    double MagicPower = 0,
    double PhysicalArmor = 0,
    double MagicResist = 0,
    double PhysicalCrit = 0,
    double HpRegen = 0,
    double EnergyRegen = 0,
    double InterruptThreshold = 0)
{
    public static HeroAttributeBundle operator +(HeroAttributeBundle left, HeroAttributeBundle right)
        => new(
            left.Strength + right.Strength,
            left.Intelligence + right.Intelligence,
            left.Agility + right.Agility,
            left.MaxHp + right.MaxHp,
            left.PhysicalAttack + right.PhysicalAttack,
            left.MagicPower + right.MagicPower,
            left.PhysicalArmor + right.PhysicalArmor,
            left.MagicResist + right.MagicResist,
            left.PhysicalCrit + right.PhysicalCrit,
            left.HpRegen + right.HpRegen,
            left.EnergyRegen + right.EnergyRegen,
            left.InterruptThreshold + right.InterruptThreshold);

    public static HeroAttributeBundle operator *(HeroAttributeBundle value, double multiplier)
        => new(
            value.Strength * multiplier,
            value.Intelligence * multiplier,
            value.Agility * multiplier,
            value.MaxHp * multiplier,
            value.PhysicalAttack * multiplier,
            value.MagicPower * multiplier,
            value.PhysicalArmor * multiplier,
            value.MagicResist * multiplier,
            value.PhysicalCrit * multiplier,
            value.HpRegen * multiplier,
            value.EnergyRegen * multiplier,
            value.InterruptThreshold * multiplier);
}

internal sealed record HeroGrowthProfile(
    string HeroId,
    BattlePrimaryAttribute PrimaryAttribute,
    double BaseStrength,
    double BaseIntelligence,
    double BaseAgility,
    double StrengthGrowth,
    double IntelligenceGrowth,
    double AgilityGrowth,
    double BaseMaxHp,
    double BasePhysicalAttack,
    double BaseMagicPower,
    double BasePhysicalArmor,
    double BaseMagicResist,
    double BasePhysicalCrit,
    double BaseHpRegen,
    double BaseEnergyRegen,
    double BaseInterruptThreshold,
    double MoveSpeed,
    double AttackRange,
    double AttackInterval,
    double UltimateRadius,
    double UltimateEnergyCost,
    double UltimateCooldown,
    double Radius,
    double AggroRange);

internal sealed record HeroProgressionState(
    int Level,
    int Star,
    HeroRank Rank,
    HeroAttributeBundle RankProgressionStats,
    HeroAttributeBundle EquippedStats);

internal readonly record struct HeroFinalStats(
    int Level,
    BattlePrimaryAttribute PrimaryAttribute,
    double Strength,
    double Intelligence,
    double Agility,
    double MaxHp,
    double PhysicalAttack,
    double MagicPower,
    double PhysicalArmor,
    double MagicResist,
    double PhysicalCrit,
    double HpRegen,
    double EnergyRegen,
    double InterruptThreshold,
    double MoveSpeed,
    double AttackRange,
    double AttackInterval,
    double UltimateDamage,
    double UltimateRadius,
    double UltimateEnergyCost,
    double UltimateCooldown,
    double Radius,
    double AggroRange)
{
    public BattleUnitSpawn ToBattleSpawn(double x, double y, int skillProfileId)
        => new(
            F(MaxHp),
            F(MoveSpeed),
            F(AttackRange),
            F(AttackInterval),
            F(PhysicalAttack),
            F(UltimateDamage),
            F(UltimateRadius),
            F(UltimateEnergyCost),
            F(EnergyRegen),
            F(UltimateCooldown),
            F(Radius),
            F(AggroRange),
            BattleControlMode.AutoCombat,
            new FixVec2(F(x), F(y)),
            skillProfileId,
            BattleUnitTag.Hero,
            PrimaryAttribute,
            Level,
            F(MagicPower),
            F(PhysicalArmor),
            F(MagicResist),
            F(PhysicalCrit),
            F(HpRegen),
            F(InterruptThreshold));

    private static Fix32 F(double value)
        => Fix32.FromDouble(Math.Max(0, value));
}

internal static class HeroAttributeCalculator
{
    public static HeroFinalStats Calculate(HeroGrowthProfile profile, HeroProgressionState progression)
    {
        int level = Math.Max(1, progression.Level);
        int star = Math.Clamp(progression.Star, 1, 5);
        HeroRankProfile rankProfile = HeroRankProfile.Get(progression.Rank);

        HeroAttributeBundle levelPrimary = new(
            Strength: profile.BaseStrength + profile.StrengthGrowth * (level - 1),
            Intelligence: profile.BaseIntelligence + profile.IntelligenceGrowth * (level - 1),
            Agility: profile.BaseAgility + profile.AgilityGrowth * (level - 1));

        double starMultiplier = 1.0 + (star - 1) * 0.085;
        HeroAttributeBundle starPrimary = levelPrimary * (starMultiplier - 1.0);
        HeroAttributeBundle rankPrimary = ApplyPrimaryWeights(
            new HeroAttributeBundle(rankProfile.MainAttribute, rankProfile.MainAttribute, rankProfile.MainAttribute),
            profile.PrimaryAttribute);

        HeroAttributeBundle beforeRankPercent = levelPrimary + starPrimary + rankPrimary + progression.RankProgressionStats + progression.EquippedStats;
        double rankMultiplier = 1.0 + rankProfile.PercentBonus;

        double strength = Math.Floor(beforeRankPercent.Strength * rankMultiplier);
        double intelligence = Math.Floor(beforeRankPercent.Intelligence * rankMultiplier);
        double agility = Math.Floor(beforeRankPercent.Agility * rankMultiplier);

        double maxHp = Math.Floor(profile.BaseMaxHp + strength * 18.0 + level * 22.0 + beforeRankPercent.MaxHp);
        double physicalAttack = Math.Floor(profile.BasePhysicalAttack + strength * 1.2 + agility * 1.8 + beforeRankPercent.PhysicalAttack);
        double magicPower = Math.Floor(profile.BaseMagicPower + intelligence * 2.4 + beforeRankPercent.MagicPower);
        double physicalArmor = Math.Floor(profile.BasePhysicalArmor + strength * 0.25 + agility * 0.18 + beforeRankPercent.PhysicalArmor);
        double magicResist = Math.Floor(profile.BaseMagicResist + intelligence * 0.22 + beforeRankPercent.MagicResist);
        double physicalCrit = Math.Floor(profile.BasePhysicalCrit + agility * 0.32 + beforeRankPercent.PhysicalCrit);
        double hpRegen = Math.Floor(profile.BaseHpRegen + strength * 0.08 + beforeRankPercent.HpRegen);
        double energyRegen = profile.BaseEnergyRegen + beforeRankPercent.EnergyRegen;
        double interruptThreshold = Math.Floor(profile.BaseInterruptThreshold + maxHp * 0.015 + beforeRankPercent.InterruptThreshold);
        double ultimateDamage = Math.Floor(profile.PrimaryAttribute == BattlePrimaryAttribute.Intelligence
            ? magicPower * 1.35 + level * 4.0
            : physicalAttack * 1.55 + level * 3.0);

        return new HeroFinalStats(
            level,
            profile.PrimaryAttribute,
            strength,
            intelligence,
            agility,
            Math.Max(1, maxHp),
            Math.Max(1, physicalAttack),
            Math.Max(0, magicPower),
            Math.Max(0, physicalArmor),
            Math.Max(0, magicResist),
            Math.Max(0, physicalCrit),
            Math.Max(0, hpRegen),
            Math.Max(0, energyRegen),
            Math.Max(0, interruptThreshold),
            profile.MoveSpeed,
            profile.AttackRange,
            profile.AttackInterval,
            Math.Max(0, ultimateDamage),
            profile.UltimateRadius,
            profile.UltimateEnergyCost,
            profile.UltimateCooldown,
            profile.Radius,
            profile.AggroRange);
    }

    public static HeroAttributeBundle CalculateRankProgressionStats(HeroRank rank, BattlePrimaryAttribute primaryAttribute)
    {
        HeroRankProfile rankProfile = HeroRankProfile.Get(rank);
        HeroAttributeBundle materialPrimary = ApplyPrimaryWeights(
            new HeroAttributeBundle(rankProfile.MaterialAttribute, rankProfile.MaterialAttribute, rankProfile.MaterialAttribute),
            primaryAttribute);
        HeroAttributeBundle materialCombat = primaryAttribute switch
        {
            BattlePrimaryAttribute.Intelligence => new HeroAttributeBundle(
                MaxHp: rankProfile.MaterialAttribute * 14,
                MagicPower: rankProfile.MaterialAttribute * 3.2,
                MagicResist: rankProfile.MaterialAttribute * 0.55),
            BattlePrimaryAttribute.Agility => new HeroAttributeBundle(
                MaxHp: rankProfile.MaterialAttribute * 15,
                PhysicalAttack: rankProfile.MaterialAttribute * 2.4,
                PhysicalCrit: rankProfile.MaterialAttribute * 0.9),
            _ => new HeroAttributeBundle(
                MaxHp: rankProfile.MaterialAttribute * 24,
                PhysicalAttack: rankProfile.MaterialAttribute * 1.5,
                PhysicalArmor: rankProfile.MaterialAttribute * 0.6),
        };

        return materialPrimary + materialCombat;
    }

    private static HeroAttributeBundle ApplyPrimaryWeights(HeroAttributeBundle value, BattlePrimaryAttribute primaryAttribute)
    {
        return primaryAttribute switch
        {
            BattlePrimaryAttribute.Intelligence => value with
            {
                Strength = value.Strength * 0.85,
                Intelligence = value.Intelligence * 1.25,
                Agility = value.Agility * 0.90,
            },
            BattlePrimaryAttribute.Agility => value with
            {
                Strength = value.Strength * 0.90,
                Intelligence = value.Intelligence * 0.85,
                Agility = value.Agility * 1.25,
            },
            _ => value with
            {
                Strength = value.Strength * 1.25,
                Intelligence = value.Intelligence * 0.85,
                Agility = value.Agility * 0.90,
            },
        };
    }

    private readonly record struct HeroRankProfile(double MainAttribute, double MaterialAttribute, double PercentBonus)
    {
        public static HeroRankProfile Get(HeroRank rank)
            => rank switch
            {
                HeroRank.White => new(0, 0, 0.00),
                HeroRank.Green => new(6, 4, 0.00),
                HeroRank.Blue => new(28, 18, 0.02),
                HeroRank.Purple => new(72, 45, 0.05),
                HeroRank.Red => new(150, 92, 0.09),
                _ => new(0, 0, 0.00),
            };
    }
}
