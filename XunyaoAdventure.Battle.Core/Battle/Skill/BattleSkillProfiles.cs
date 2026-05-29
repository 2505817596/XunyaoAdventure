namespace LeanClr.Battle;

public static partial class BattleSkillProfiles
{
    public const int Default = 0;
    public const int NoBasicAttackStun = 1;
    public const int LongBasicAttackStun = 2;
    public const int HealerSupport = 3;
    public const int ShieldSupport = 4;
    public const int ThornsTank = 5;
    public const int LifeStealDuelist = 6;
    public const int DispelSupport = 7;
    public const int RageGuard = 8;
    public const int SecondWindHealer = 9;
    public const int DeathBurstHunter = 10;
    public const int SlowMage = 11;
    public const int TripleStrikeDuelist = 12;
    public const int EnergySupport = 13;
    public const int TauntTank = 14;
    public const int PullMage = 15;
    public const int RevivePriest = 16;
    public const int Summoner = 17;
    public const int DeathSummonHunter = 18;
    public const int BurningMage = 19;
    public const int LastStandTank = 20;
    public const int BattleStartCleric = 21;
    public const int KillFighter = 22;
    public const int DamageTakenGuard = 23;
    public const int DamageDealtStriker = 24;
    public const int AllyDeathGuard = 25;
    public const int EnemyDeathCleric = 26;
    public const int SkillStartSage = 27;
    public const int SkillFinishSage = 28;
    public const int HealReceivedPriest = 29;
    public const int HealGrantedCleric = 30;
    public const int SkillHitArcanist = 31;
    public const int AuraBanner = 32;
    public const int WarDrumAura = 33;
    public const int FrostCurseAura = 34;
    public const int GuardianAura = 35;
    public const int LinePiercer = 36;
    public const int FanBreaker = 37;
    public const int ChainLightningMage = 38;
    public const int BacklineAssassin = 39;
    public const int MidlineSilencer = 40;
    public const int FrontlineProtector = 41;
    public const int MultiShotArcher = 42;
    public const int ScatterVolley = 43;
    public const int ArcaneExecutioner = 44;
    public const int PlainDamageTest = 45;

    private static readonly BattleSkillProfile[] Profiles = CreateProfiles();

    public static IReadOnlyList<BattleSkillProfile> All => Profiles;

    public static BattleSkillProfile Resolve(int id)
    {
        if ((uint)id < (uint)Profiles.Length)
        {
            return Profiles[id];
        }

        return Profiles[Default];
    }

    public static BattleConfigValidationResult Validate()
    {
        if (Profiles.Length == 0)
        {
            return BattleConfigValidationResult.Fail("No skill profiles configured.");
        }

        for (int i = 0; i < Profiles.Length; i++)
        {
            BattleConfigValidationResult result = BattleConfigValidator.ValidateSkillProfile(i, Profiles[i]);
            if (!result.IsValid)
            {
                return result;
            }
        }

        return BattleConfigValidationResult.Success();
    }

    private static BattleSkillProfile[] CreateProfiles()
        => new[]
        {
            CreateDefaultProfile(),
            CreateNoBasicAttackStunProfile(),
            CreateLongBasicAttackStunProfile(),
            CreateHealerSupportProfile(),
            CreateShieldSupportProfile(),
            CreateThornsTankProfile(),
            CreateLifeStealDuelistProfile(),
            CreateDispelSupportProfile(),
            CreateRageGuardProfile(),
            CreateSecondWindHealerProfile(),
            CreateDeathBurstHunterProfile(),
            CreateSlowMageProfile(),
            CreateTripleStrikeDuelistProfile(),
            CreateEnergySupportProfile(),
            CreateTauntTankProfile(),
            CreatePullMageProfile(),
            CreateRevivePriestProfile(),
            CreateSummonerProfile(),
            CreateDeathSummonHunterProfile(),
            CreateBurningMageProfile(),
            CreateLastStandTankProfile(),
            CreateBattleStartClericProfile(),
            CreateKillFighterProfile(),
            CreateDamageTakenGuardProfile(),
            CreateDamageDealtStrikerProfile(),
            CreateAllyDeathGuardProfile(),
            CreateEnemyDeathClericProfile(),
            CreateSkillStartSageProfile(),
            CreateSkillFinishSageProfile(),
            CreateHealReceivedPriestProfile(),
            CreateHealGrantedClericProfile(),
            CreateSkillHitArcanistProfile(),
            CreateAuraBannerProfile(),
            CreateWarDrumAuraProfile(),
            CreateFrostCurseAuraProfile(),
            CreateGuardianAuraProfile(),
            CreateLinePiercerProfile(),
            CreateFanBreakerProfile(),
            CreateChainLightningMageProfile(),
            CreateBacklineAssassinProfile(),
            CreateMidlineSilencerProfile(),
            CreateFrontlineProtectorProfile(),
            CreateMultiShotArcherProfile(),
            CreateScatterVolleyProfile(),
            CreateArcaneExecutionerProfile(),
            CreatePlainDamageTestProfile(),
        };
}

