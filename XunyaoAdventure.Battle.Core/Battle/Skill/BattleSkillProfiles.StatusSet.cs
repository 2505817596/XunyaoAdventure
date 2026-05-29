using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static partial class BattleSkillProfiles
{
    private static BattleSkillProfile CreateBurningMageProfile()
        => new(
            BurningMage,
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
                BattleSkillEffectTemplate.DamageOverTimePrimary(BattleSkillValueSource.PhysicalAttack, Fix32.FromDouble(0.75), Fix32.FromDouble(2.25)),
            },
            new[]
            {
                BattleSkillEffectTemplate.DamageEnemiesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius)
                    .ExcludingTargetUnitTag(BattleUnitTag.Summon),
                BattleSkillEffectTemplate.InterruptEnemiesInRadius(BattleSkillValueSource.UltimateRadius)
                    .ExcludingTargetUnitTag(BattleUnitTag.Summon),
                BattleSkillEffectTemplate.DamageOverTimeEnemiesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.UltimateRadius, Fix32.FromDouble(0.75), Fix32.FromDouble(2.25))
                    .ExcludingTargetUnitTag(BattleUnitTag.Summon),
                BattleSkillEffectTemplate.ExecuteEnemiesInRadius(Fix32.FromDouble(0.12), BattleSkillValueSource.UltimateRadius)
                    .ExcludingTargetUnitTag(BattleUnitTag.Summon),
            });

    private static BattleSkillProfile CreateLastStandTankProfile()
        => new(
            LastStandTank,
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
                BattleSkillEffectTemplate.PhysicalImmuneSelf(Fix32.FromDouble(1.2)),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.HealthBelow(
                    Fix32.FromDouble(0.35),
                    new[]
                    {
                        BattleSkillEffectTemplate.ShieldSelf(BattleSkillValueSource.UltimateDamage, Fix32.FromDouble(4.0)),
                        BattleSkillEffectTemplate.DamageImmuneSelf(Fix32.FromDouble(0.8)),
                }),
            });

    private static BattleSkillProfile CreateBattleStartClericProfile()
        => new(
            BattleStartCleric,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.BattleStart(new[]
                {
                    BattleSkillEffectTemplate.ShieldPrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
                    BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.UltimateRadius)
                        .WithTags(BattleSkillTag.Heal | BattleSkillTag.Buff),
                }),
            });

    private static BattleSkillProfile CreateKillFighterProfile()
        => new(
            KillFighter,
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
                BattleSkillEffectTemplate.ShieldPrimary(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.KillEnemy(new[]
                {
                    BattleSkillEffectTemplate.ShieldSelf(BattleSkillValueSource.PhysicalAttack, Fix32.FromDouble(3.0)),
                }),
            });

    private static BattleSkillProfile CreateDamageTakenGuardProfile()
        => new(
            DamageTakenGuard,
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
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.DamageTaken(new[]
                {
                    BattleSkillEffectTemplate.ShieldSelf(BattleSkillValueSource.PhysicalAttack, Fix32.FromDouble(1.2)),
                }),
            });

    private static BattleSkillProfile CreateDamageDealtStrikerProfile()
        => new(
            DamageDealtStriker,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.12),
            Fix32.FromDouble(0.4),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
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
                BattleSkillEffectTemplate.SlowPrimary(Fix32.FromDouble(0.35), Fix32.FromDouble(1.0)),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.DamageDealt(new[]
                {
                    BattleSkillEffectTemplate.ShieldSelf(BattleSkillValueSource.PhysicalAttack, Fix32.FromDouble(0.8)),
                }),
            });

    private static BattleSkillProfile CreateAllyDeathGuardProfile()
        => new(
            AllyDeathGuard,
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
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.AllyDied(new[]
                {
                    BattleSkillEffectTemplate.ShieldSelf(BattleSkillValueSource.UltimateDamage, Fix32.FromDouble(2.0)),
                }),
            });

    private static BattleSkillProfile CreateEnemyDeathClericProfile()
        => new(
            EnemyDeathCleric,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.EnemyDied(new[]
                {
                    BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.UltimateRadius),
                }),
            });

    private static BattleSkillProfile CreateSkillStartSageProfile()
        => new(
            SkillStartSage,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.SkillStart(new[]
                {
                    BattleSkillEffectTemplate.ShieldSelf(BattleSkillValueSource.UltimateDamage, Fix32.FromDouble(1.0)),
                }),
            });

    private static BattleSkillProfile CreateSkillFinishSageProfile()
        => new(
            SkillFinishSage,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.SkillFinish(new[]
                {
                    BattleSkillEffectTemplate.ModifyEnergyPrimary(BattleSkillValueSource.UltimateDamage),
                }),
            });

    private static BattleSkillProfile CreateHealReceivedPriestProfile()
        => new(
            HealReceivedPriest,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.HealReceived(new[]
                {
                    BattleSkillEffectTemplate.ShieldSelf(BattleSkillValueSource.UltimateDamage, Fix32.FromDouble(2.0)),
                }),
            });

    private static BattleSkillProfile CreateHealGrantedClericProfile()
        => new(
            HealGrantedCleric,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.HealGranted(new[]
                {
                    BattleSkillEffectTemplate.ModifyEnergyPrimary(BattleSkillValueSource.PhysicalAttack),
                }),
            });

    private static BattleSkillProfile CreateSkillHitArcanistProfile()
        => new(
            SkillHitArcanist,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
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
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.UltimateDamage),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.SkillHit(new[]
                {
                    BattleSkillEffectTemplate.ShieldSelf(BattleSkillValueSource.UltimateDamage, Fix32.FromDouble(1.5)),
                }),
            });

    private static BattleSkillProfile CreateAuraBannerProfile()
        => new(
            AuraBanner,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.HealAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.AuraTick(
                    Fix32.FromDouble(2.0),
                    new[]
                    {
                        BattleSkillEffectTemplate.ShieldAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.Constant, BattleSkillValueSource.UltimateRadius),
                    }),
            });

    private static BattleSkillProfile CreateWarDrumAuraProfile()
        => new(
            WarDrumAura,
            Fix32.FromDouble(0.35),
            Fix32.FromDouble(0.15),
            Fix32.FromDouble(0.45),
            Fix32.FromRaw(3277),
            Fix32.Zero,
            BattleProjectileMotionType.None,
            Fix32.FromDouble(0.3),
            Fix32.FromDouble(0.3),
            BattleSkillTargetTeam.Enemy,
            BattleSkillTargetTeam.Ally,
            new[]
            {
                BattleSkillEffectTemplate.DamagePrimary(BattleSkillValueSource.PhysicalAttack),
            },
            new[]
            {
                BattleSkillEffectTemplate.PhysicalAttackUpAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius, BattleSkillValueSource.UltimateRadius),
                BattleSkillEffectTemplate.AttackSpeedUpAlliesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.UltimateRadius, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.AuraTick(
                    Fix32.FromDouble(2.6),
                    new[]
                    {
                        BattleSkillEffectTemplate.PhysicalAttackUpAlliesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.Constant, BattleSkillValueSource.UltimateRadius),
                    }),
            });

    private static BattleSkillProfile CreateFrostCurseAuraProfile()
        => new(
            FrostCurseAura,
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
                BattleSkillEffectTemplate.SlowPrimary(Fix32.FromDouble(0.25), Fix32.FromDouble(1.5)),
            },
            new[]
            {
                BattleSkillEffectTemplate.AttackSpeedDownEnemiesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.UltimateRadius, BattleSkillValueSource.UltimateRadius),
                BattleSkillEffectTemplate.MagicResistDownEnemiesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.UltimateRadius, BattleSkillValueSource.UltimateRadius),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.AuraTick(
                    Fix32.FromDouble(2.2),
                    new[]
                    {
                        BattleSkillEffectTemplate.AttackSpeedDownEnemiesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.Constant, BattleSkillValueSource.UltimateRadius),
                    }),
            });

    private static BattleSkillProfile CreateGuardianAuraProfile()
        => new(
            GuardianAura,
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
                BattleSkillEffectTemplate.PhysicalArmorUpAlliesInRadius(BattleSkillValueSource.UltimateDamage, BattleSkillValueSource.UltimateRadius, BattleSkillValueSource.UltimateRadius),
                BattleSkillEffectTemplate.HealOverTimeAlliesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.UltimateRadius, Fix32.FromDouble(0.75), Fix32.FromDouble(2.25)),
            },
            passiveTriggers: new[]
            {
                BattlePassiveTriggerTemplate.AuraTick(
                    Fix32.FromDouble(2.3),
                    new[]
                    {
                        BattleSkillEffectTemplate.PhysicalArmorUpAlliesInRadius(BattleSkillValueSource.PhysicalAttack, BattleSkillValueSource.Constant, BattleSkillValueSource.UltimateRadius),
                    }),
            });
}


