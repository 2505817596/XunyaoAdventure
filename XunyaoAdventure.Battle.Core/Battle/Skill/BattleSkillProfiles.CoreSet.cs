using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static partial class BattleSkillProfiles
{
    private static BattleSkillProfile CreateDefaultProfile()
        => new(
            Default,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
                BattleSkillEffectTemplate.StunPrimary(Fix32.Half),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageEnemiesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateNoBasicAttackStunProfile()
        => new(
            NoBasicAttackStun,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.FromInt(7),
            BattleProjectileMotionType.Homing,
            Fix32.FromDouble(0.25),
            Fix32.FromDouble(0.25),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageEnemiesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            new[]
            {
                new BattleSkillTimelineEventTemplate(
                    Fix32.FromDouble(0.35),
                    Fix32.Zero,
                    Fix32.FromInt(5),
                    BattleProjectileMotionType.Linear,
                    BattleProjectileImpactType.AreaAtDestination,
                    new[]
                    {
                        BattleSkillEffectTemplate.DamageEnemiesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
                    }),
            });

    private static BattleSkillProfile CreatePlainDamageTestProfile()
        => new(
            PlainDamageTest,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.UltimateDamage),
            });

    private static BattleSkillProfile CreateLongBasicAttackStunProfile()
        => new(
            LongBasicAttackStun,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.6),
            Fix32.FromDouble(0.45),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
                BattleSkillEffectTemplate.StunPrimary(Fix32.One),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageEnemiesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateHealerSupportProfile()
        => new(
            HealerSupport,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.45),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.PhysicalArmorUpPrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateShieldSupportProfile()
        => new(
            ShieldSupport,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.45),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.ShieldPrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
                BattleSkillEffectTemplate.DamageTakenAmplifyPrimary(Fix32.FromDouble(1.25), Fix32.FromDouble(3.0))
                    .ExcludingTargetUnitTag(BattleUnitTag.Summon),
            });

    private static BattleSkillProfile CreateThornsTankProfile()
        => new(
            ThornsTank,
            Fix32.FromDouble(0.4),
            Fix32.FromDouble(0.2),
            Fix32.FromDouble(0.5),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.45),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.ThornsPrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateLifeStealDuelistProfile()
        => new(
            LifeStealDuelist,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.12),
            Fix32.FromDouble(0.4),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.25),
            Fix32.FromDouble(0.25),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.LifeStealPrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateDispelSupportProfile()
        => new(
            DispelSupport,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.25),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.CleansePrimary(2),
                BattleSkillEffectTemplate.PurgePrimary(1),
            });

    private static BattleSkillProfile CreateRageGuardProfile()
        => new(
            RageGuard,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.RagePrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateSecondWindHealerProfile()
        => new(
            SecondWindHealer,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.4),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.SecondWindPrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateDeathBurstHunterProfile()
        => new(
            DeathBurstHunter,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DeathBurstPrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateSlowMageProfile()
        => new(
            SlowMage,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.FromInt(6),
            BattleProjectileMotionType.Linear,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
                BattleSkillEffectTemplate.SlowPrimary(Fix32.FromDouble(0.35), Fix32.FromDouble(1.5)),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageEnemiesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateTripleStrikeDuelistProfile()
        => new(
            TripleStrikeDuelist,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.12),
            Fix32.FromDouble(0.4),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.2),
            Fix32.FromDouble(0.5),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.UltimateDamage),
            },
            new[]
            {
                new BattleSkillTimelineEventTemplate(
                    Fix32.FromDouble(0.2),
                    Fix32.Zero,
                    new[]
                    {
                        BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
                    }),
                new BattleSkillTimelineEventTemplate(
                    Fix32.FromDouble(0.4),
                    Fix32.Zero,
                    new[]
                    {
                        BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
                        BattleSkillEffectTemplate.SlowPrimary(Fix32.FromDouble(0.35), Fix32.FromDouble(1.2)),
                    }),
                new BattleSkillTimelineEventTemplate(
                    Fix32.FromDouble(0.6),
                    Fix32.Zero,
                    new[]
                    {
                        BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.UltimateDamage),
                        BattleSkillEffectTemplate.StunPrimary(Fix32.FromDouble(0.5)),
                    }),
            });

    private static BattleSkillProfile CreateEnergySupportProfile()
        => new(
            EnergySupport,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.ModifyEnergyPrimary(BattleSkillValueSource.UltimateDamage),
                BattleSkillEffectTemplate.SilenceEnemiesInRadius(Fix32.FromDouble(1.0), BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateTauntTankProfile()
        => new(
            TauntTank,
            Fix32.FromDouble(0.4),
            Fix32.FromDouble(0.2),
            Fix32.FromDouble(0.5),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.45),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.TauntSelf(Fix32.FromDouble(2.0)),
            });

    private static BattleSkillProfile CreatePullMageProfile()
        => new(
            PullMage,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.FromInt(5),
            BattleProjectileMotionType.Linear,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DisplacePrimary(Fix32.FromDouble(-1.0)),
            });

    private static BattleSkillProfile CreateRevivePriestProfile()
        => new(
            RevivePriest,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.RevivePrimary(BattleSkillValueSource.UltimateDamage),
                BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateSummonerProfile()
        => new(
            Summoner,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.4),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.SummonPrimary(Fix32.FromDouble(0.6), NoBasicAttackStun, Fix32.FromDouble(8.0)),
                BattleSkillEffectTemplate.ShieldPrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateDeathSummonHunterProfile()
        => new(
            DeathSummonHunter,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.UltimateDamage),
            },
            deathEffects: new[]
            {
                BattleSkillEffectTemplate.SummonSelf(Fix32.FromDouble(0.5), Summoner, Fix32.FromDouble(6.0)),
            });
}


