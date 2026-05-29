using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static partial class BattleSkillProfiles
{
    private static BattleSkillProfile CreateLinePiercerProfile()
        => new(
            LinePiercer,
            Fix32.FromDouble(0.32),
            Fix32.FromDouble(0.12),
            Fix32.FromDouble(0.4),
            Fix32.FromRaw(3277),
            Fix32.FromInt(8),
            BattleProjectileMotionType.Linear,
            Fix32.FromDouble(0.25),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageEnemiesInLine(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
                BattleSkillEffectTemplate.StunEnemiesInLine(Fix32.FromDouble(0.35), BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateFanBreakerProfile()
        => new(
            FanBreaker,
            Fix32.FromDouble(0.4),
            Fix32.FromDouble(0.15),
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
                BattleSkillEffectTemplate.DamageEnemiesInFan(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
                BattleSkillEffectTemplate.SlowEnemiesInFan(Fix32.FromDouble(0.3), Fix32.FromDouble(1.2), BattleSkillValueSource.UltimateRadius),
            });

    private static BattleSkillProfile CreateChainLightningMageProfile()
        => new(
            ChainLightningMage,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.FromInt(7),
            BattleProjectileMotionType.Homing,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageChainedEnemies(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius, 4),
                BattleSkillEffectTemplate.SilenceChainedEnemies(Fix32.FromDouble(0.7), BattleSkillValueSource.UltimateRadius, 4),
            });

    private static BattleSkillProfile CreateBacklineAssassinProfile()
        => new(
            BacklineAssassin,
            Fix32.FromDouble(0.28),
            Fix32.FromDouble(0.1),
            Fix32.FromDouble(0.38),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.25),
            Fix32.FromDouble(0.28),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageBackEnemy(BattleSkillValueSource.UltimateDamage),
                BattleSkillEffectTemplate.StunBackEnemy(Fix32.FromDouble(0.45)),
            });

    private static BattleSkillProfile CreateMidlineSilencerProfile()
        => new(
            MidlineSilencer,
            Fix32.FromDouble(0.34),
            Fix32.FromDouble(0.14),
            Fix32.FromDouble(0.44),
            Fix32.FromRaw(3277),
            Fix32.FromInt(6),
            BattleProjectileMotionType.Linear,
            Fix32.FromDouble(0.32),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageMiddleEnemyRow(BattleSkillValueSource.UltimateDamage),
                BattleSkillEffectTemplate.SilenceMiddleEnemyRow(Fix32.FromDouble(0.9)),
            });

    private static BattleSkillProfile CreateFrontlineProtectorProfile()
        => new(
            FrontlineProtector,
            Fix32.FromDouble(0.4),
            Fix32.FromDouble(0.18),
            Fix32.FromDouble(0.48),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.4),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.ShieldFrontAllyRow(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
                BattleSkillEffectTemplate.HealFrontAllyRow(BattleSkillValueSource.PhysicalAttack),
            });

    private static BattleSkillProfile CreateMultiShotArcherProfile()
        => new(
            MultiShotArcher,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.12),
            Fix32.FromDouble(0.38),
            Fix32.FromRaw(3277),
            Fix32.FromInt(7),
            BattleProjectileMotionType.Homing,
            Fix32.FromDouble(0.25),
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
                BattleSkillEffectTemplate.SlowPrimary(Fix32.FromDouble(0.25), Fix32.FromDouble(1.0)),
            },
            new[]
            {
                new BattleSkillTimelineEventTemplate(
                    Fix32.FromDouble(0.25),
                    Fix32.Zero,
                    Fix32.FromInt(8),
                    BattleProjectileMotionType.Homing,
                    BattleProjectileImpactType.PrimaryTarget,
                    BattleProjectileTargetMode.NearestEnemies,
                    3,
                    new[]
                    {
                        BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.UltimateDamage),
                        BattleSkillEffectTemplate.SlowPrimary(Fix32.FromDouble(0.25), Fix32.FromDouble(1.0)),
                    }),
            });

    private static BattleSkillProfile CreateScatterVolleyProfile()
        => new(
            ScatterVolley,
            Fix32.FromDouble(0.34),
            Fix32.FromDouble(0.14),
            Fix32.FromDouble(0.42),
            Fix32.FromRaw(3277),
            Fix32.FromInt(6),
            BattleProjectileMotionType.Linear,
            Fix32.FromDouble(0.28),
            Fix32.FromDouble(0.38),
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
                    Fix32.FromDouble(0.28),
                    Fix32.Zero,
                    Fix32.FromInt(7),
                    BattleProjectileMotionType.Linear,
                    BattleProjectileImpactType.AreaAtDestination,
                    BattleProjectileTargetMode.RandomEnemies,
                    4,
                    new[]
                    {
                        BattleSkillEffectTemplate.DamageEnemiesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
                    }),
            });

    private static BattleSkillProfile CreateArcaneExecutionerProfile()
        => new(
            ArcaneExecutioner,
            Fix32.FromDouble(0.32),
            Fix32.FromDouble(0.12),
            Fix32.FromDouble(0.4),
            Fix32.FromRaw(3277),
            Fix32.FromInt(7),
            BattleProjectileMotionType.Homing,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.35),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Enemy,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.FormulaDamagePrimary(
                    BattleDamageType.Magical,
                    new BattleFormulaSpec(new[]
                    {
                        BattleFormulaTerm.SourceValue(BattleSkillValueSource.UltimateDamage, Fix32.One),
                        BattleFormulaTerm.SourceValue(BattleSkillValueSource.TargetMissingHp, Fix32.FromDouble(0.18)),
                    }))
                    .OnlyIfTargetHpBelow(Fix32.FromDouble(0.5)),
            },
            autoSkillTemplates: new[]
            {
                new BattleSkillConfigTemplate(
                    BattleSkillSlot.AutoSkill1,
                    BattleSkillType.AutoSkill,
                    BattleSkillTargetTeam.Enemy,
                    Fix32.FromInt(7),
                    Fix32.FromDouble(0.25),
                    Fix32.FromDouble(0.3),
                    Fix32.FromDouble(4.0),
                    false,
                    new[]
                    {
                        BattleSkillEffectTemplate.FormulaDamagePrimary(
                            BattleDamageType.Magical,
                            new BattleFormulaSpec(new[]
                            {
                                BattleFormulaTerm.SourceValue(BattleSkillValueSource.PhysicalAttack, Fix32.FromDouble(1.2)),
                                BattleFormulaTerm.SourceValue(BattleSkillValueSource.TargetMaxHp, Fix32.FromDouble(0.05)),
                            }))
                            .OnlyIfTargetIsControlled(),
                        BattleSkillEffectTemplate.SilencePrimary(Fix32.FromDouble(0.6)),
                    }),
            });
}


