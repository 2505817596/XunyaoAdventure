using System;
using System.IO;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal static class BattleRegressionTests
{
    private const int FrameCount = 180;

    public static int RunAll()
    {
        try
        {
            RunConfigValidation();
            RunCapabilitySnapshotCheck();
            RunSetupWireRoundTrip();
            RunEventWireRoundTrip();
            RunFormationTargetingRegression();
            RunPrimaryAttributeRowTargetingRegression();
            RunDamageMitigationDoesNotOverflowRegression();
            RunMeleeBasicAttackDoesNotCrossLayerRegression();
            RunRangedBasicAttackDoesNotCrossLayerRegression();
            RunDeterministicReplay();
            Console.WriteLine("Battle regression tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Battle regression tests failed: " + ex.Message);
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void RunConfigValidation()
    {
        BattleConfigValidationResult validation = BattleConfigValidator.ValidateAll();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("Config validation failed: " + validation.Message);
        }
    }

    private static void RunCapabilitySnapshotCheck()
    {
        string snapshotPath = BattleSkillCapabilitySnapshot.GetGoldenFilePath();
        string expected = File.ReadAllText(snapshotPath);
        BattleConfigValidationResult result = BattleSkillCapabilitySnapshot.ValidateAgainst(expected);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(result.Message);
        }
    }

    private static void RunSetupWireRoundTrip()
    {
        BattleEncounterSetup setup = CreateReplaySetup();
        byte[] encoded = BattleSetupWire.EncodeSetup(setup);
        BattleEncounterSetup decoded = BattleSetupWire.DecodeSetup(encoded);

        if (decoded.PlayerUnits.Count != setup.PlayerUnits.Count || decoded.EnemyWaves.Count != setup.EnemyWaves.Count)
        {
            throw new InvalidOperationException("Setup wire round-trip count mismatch.");
        }

        byte[] reencoded = BattleSetupWire.EncodeSetup(decoded);
        AssertByteArraysEqual(encoded, reencoded, "setup wire round-trip");
    }

    private static void RunEventWireRoundTrip()
    {
        BattleFrameOutput output = RunReplay()[0];
        byte[] encoded = BattleWire.EncodeEventBatch(output);
        BattleFrameOutput decoded = BattleWire.DecodeEventBatch(encoded);

        if (decoded.FrameNo != output.FrameNo ||
            decoded.StateHash != output.StateHash ||
            decoded.Events.Length != output.Events.Length ||
            decoded.UnitStates.Length != output.UnitStates.Length)
        {
            throw new InvalidOperationException("Event wire round-trip header mismatch.");
        }

        byte[] reencoded = BattleWire.EncodeEventBatch(decoded);
        AssertByteArraysEqual(encoded, reencoded, "event wire round-trip");
    }

    private static void RunDeterministicReplay()
    {
        BattleFrameOutput[] first = RunReplay();
        BattleFrameOutput[] second = RunReplay();

        for (int i = 0; i < first.Length; i++)
        {
            AssertFrameEqual(first[i], second[i]);
        }
    }

    private static void RunFormationTargetingRegression()
    {
        BattleEncounterSetup setup = new();
        AddPlayer(setup, 0.0, 0.0, BattleSkillProfiles.MultiShotArcher);
        BattleWaveSetup wave = setup.CreateEnemyWave();
        if (!wave.TryAddUnit(CreateSpawn(2.0, 1.6, 80, 1.4, 1.0, 10, 24, BattleSkillProfiles.Default)))
        {
            throw new InvalidOperationException("Failed to add front-layer enemy.");
        }

        if (!wave.TryAddUnit(CreateSpawn(2.4, 0.0, 80, 1.4, 1.0, 10, 24, BattleSkillProfiles.Default)))
        {
            throw new InvalidOperationException("Failed to add second-layer enemy.");
        }

        BattleHost host = new(setup);
        host.Start();

        host.Step(new BattleFrameInput(0, Fix32.FromDouble(0.2), Array.Empty<BattleCommandEnvelope>()));
        BattleUnit source = host.World.Units[1];
        if (source.CurrentTargetId != 2)
        {
            throw new InvalidOperationException($"Formation targeting regression failed: expected unit 1 to target unit 2, got {source.CurrentTargetId}.");
        }
    }

    private static void RunRangedBasicAttackDoesNotCrossLayerRegression()
    {
        BattleEncounterSetup setup = new();
        AddPlayer(setup, 0.0, 0.0, BattleSkillProfiles.MultiShotArcher);
        BattleWaveSetup wave = setup.CreateEnemyWave();
        if (!wave.TryAddUnit(CreateSpawn(3.0, 1.6, 80, 1.4, 1.0, 10, 24, BattleSkillProfiles.Default)))
        {
            throw new InvalidOperationException("Failed to add ranged target enemy.");
        }

        BattleHost host = new(setup);
        host.Start();

        bool attacked = false;
        bool damaged = false;
        for (int frameNo = 0; frameNo <= 4 && (!attacked || !damaged); frameNo++)
        {
            BattleFrameOutput frame = host.Step(new BattleFrameInput(frameNo, Fix32.FromDouble(0.2), Array.Empty<BattleCommandEnvelope>()));
            BattleUnit source = host.World.Units[1];
            if (source.Position.Y != Fix32.Zero)
            {
                throw new InvalidOperationException($"Ranged layer regression failed: expected y=0, got {source.Position.Y.Raw}.");
            }

            foreach (BattleEvent battleEvent in frame.Events)
            {
                if (battleEvent.Type == BattleEventType.UnitAttack && battleEvent.UnitId == 1 && battleEvent.TargetUnitId == 2)
                {
                    attacked = true;
                }

                if (battleEvent.Type == BattleEventType.UnitDamaged && battleEvent.UnitId == 2 && battleEvent.TargetUnitId == 1 && battleEvent.Amount > Fix32.Zero)
                {
                    damaged = true;
                }
            }
        }

        if (!attacked)
        {
            throw new InvalidOperationException("Ranged layer regression failed: ranged unit should attack without changing layer.");
        }

        if (!damaged)
        {
            throw new InvalidOperationException("Ranged layer regression failed: ranged unit should damage cross-layer target.");
        }
    }

    private static void RunMeleeBasicAttackDoesNotCrossLayerRegression()
    {
        BattleEncounterSetup setup = new();
        AddPlayer(setup, 0.0, 0.0, BattleSkillProfiles.Default);
        BattleWaveSetup wave = setup.CreateEnemyWave();
        if (!wave.TryAddUnit(CreateSpawn(1.2, 1.0, 80, 1.4, 1.0, 10, 24, BattleSkillProfiles.Default)))
        {
            throw new InvalidOperationException("Failed to add melee target enemy.");
        }

        BattleHost host = new(setup);
        host.Start();

        bool attacked = false;
        bool damaged = false;
        for (int frameNo = 0; frameNo <= 4 && (!attacked || !damaged); frameNo++)
        {
            BattleFrameOutput frame = host.Step(new BattleFrameInput(frameNo, Fix32.FromDouble(0.2), Array.Empty<BattleCommandEnvelope>()));
            BattleUnit source = host.World.Units[1];
            if (source.Position.Y != Fix32.Zero)
            {
                throw new InvalidOperationException($"Melee layer regression failed: expected y=0, got {source.Position.Y.Raw}.");
            }

            foreach (BattleEvent battleEvent in frame.Events)
            {
                if (battleEvent.Type == BattleEventType.UnitAttack && battleEvent.UnitId == 1 && battleEvent.TargetUnitId == 2)
                {
                    attacked = true;
                }

                if (battleEvent.Type == BattleEventType.UnitDamaged && battleEvent.UnitId == 2 && battleEvent.TargetUnitId == 1 && battleEvent.Amount > Fix32.Zero)
                {
                    damaged = true;
                }
            }
        }

        if (!attacked)
        {
            throw new InvalidOperationException("Melee layer regression failed: melee unit should attack when x range is enough.");
        }

        if (!damaged)
        {
            throw new InvalidOperationException("Melee layer regression failed: melee unit should damage cross-layer target.");
        }
    }

    private static void RunPrimaryAttributeRowTargetingRegression()
    {
        BattleEncounterSetup setup = new();
        if (!setup.TryAddPlayerUnit(CreateSpawn(
            0.0,
            0.0,
            100,
            2.0,
            3.2,
            10,
            28,
            BattleSkillProfiles.BacklineAssassin,
            BattlePrimaryAttribute.Agility,
            Fix32.Zero)))
        {
            throw new InvalidOperationException("Failed to add backline targeting player.");
        }
        BattleWaveSetup wave = setup.CreateEnemyWave();
        if (!wave.TryAddUnit(CreateSpawn(2.0, 0.0, 80, 1.4, 1.0, 10, 24, BattleSkillProfiles.Default, BattlePrimaryAttribute.Strength)))
        {
            throw new InvalidOperationException("Failed to add strength enemy.");
        }

        if (!wave.TryAddUnit(CreateSpawn(2.4, 0.0, 80, 1.4, 1.0, 10, 24, BattleSkillProfiles.Default, BattlePrimaryAttribute.Intelligence)))
        {
            throw new InvalidOperationException("Failed to add intelligence enemy.");
        }

        if (!wave.TryAddUnit(CreateSpawn(2.8, 0.0, 80, 1.4, 1.0, 10, 24, BattleSkillProfiles.Default, BattlePrimaryAttribute.Agility)))
        {
            throw new InvalidOperationException("Failed to add agility enemy.");
        }

        BattleHost host = new(setup);
        host.Start();
        host.Step(new BattleFrameInput(0, Fix32.FromDouble(0.2), new[] { new BattleCommandEnvelope(1, BattleCommand.CastUltimate()) }));

        bool hitAgility = false;
        for (int frameNo = 1; frameNo <= 4 && !hitAgility; frameNo++)
        {
            BattleFrameOutput frame = host.Step(new BattleFrameInput(frameNo, Fix32.FromDouble(0.2), Array.Empty<BattleCommandEnvelope>()));
            foreach (BattleEvent battleEvent in frame.Events)
            {
                if (battleEvent.Type == BattleEventType.UnitDamaged && battleEvent.UnitId == 4)
                {
                    hitAgility = true;
                    break;
                }
            }
        }

        if (!hitAgility)
        {
            throw new InvalidOperationException("Primary attribute row targeting failed: BackEnemy should select Agility.");
        }
    }

    private static void RunDamageMitigationDoesNotOverflowRegression()
    {
        BattleEncounterSetup setup = new();
        if (!setup.TryAddPlayerUnit(CreateSpawn(
            0.0,
            0.0,
            1000,
            2.0,
            1.2,
            120,
            120,
            BattleSkillProfiles.PlainDamageTest,
            physicalArmor: Fix32.FromDouble(24))))
        {
            throw new InvalidOperationException("Failed to add overflow regression player.");
        }

        BattleWaveSetup wave = setup.CreateEnemyWave();
        if (!wave.TryAddUnit(CreateSpawn(
            1.1,
            0.0,
            1000,
            2.0,
            1.2,
            120,
            120,
            BattleSkillProfiles.PlainDamageTest,
            physicalArmor: Fix32.FromDouble(24))))
        {
            throw new InvalidOperationException("Failed to add overflow regression enemy.");
        }

        BattleHost host = new(setup);
        host.Start();

        for (int frameNo = 0; frameNo <= 4; frameNo++)
        {
            BattleFrameOutput frame = host.Step(new BattleFrameInput(frameNo, Fix32.FromDouble(0.2), Array.Empty<BattleCommandEnvelope>()));
            foreach (BattleEvent battleEvent in frame.Events)
            {
                if (battleEvent.Type == BattleEventType.UnitDamaged && battleEvent.Amount > Fix32.Zero)
                {
                    return;
                }
            }
        }

        throw new InvalidOperationException("Damage mitigation overflow regression failed: mitigated damage should stay positive.");
    }

    private static BattleFrameOutput[] RunReplay()
    {
        BattleHost host = new(CreateReplaySetup());
        BattleFrameOutput[] frames = new BattleFrameOutput[FrameCount];
        for (int frameNo = 0; frameNo < frames.Length; frameNo++)
        {
            BattleFrameInput input = new(frameNo, Fix32.FromDouble(0.2), BuildCommandsForFrame(frameNo));
            BattleFrameInput decodedInput = BattleWire.DecodeCommandBatch(BattleWire.EncodeCommandBatch(input));
            frames[frameNo] = host.Step(decodedInput);
        }

        return frames;
    }

    private static BattleCommandEnvelope[] BuildCommandsForFrame(int frameNo)
    {
        return frameNo switch
        {
            6 => new[] { new BattleCommandEnvelope(1, BattleCommand.CastUltimate()) },
            12 => new[] { new BattleCommandEnvelope(2, BattleCommand.CastUltimate()) },
            18 => new[] { new BattleCommandEnvelope(3, BattleCommand.CastUltimate()) },
            24 => new[] { new BattleCommandEnvelope(4, BattleCommand.CastUltimate()) },
            30 => new[] { new BattleCommandEnvelope(5, BattleCommand.CastUltimate()) },
            36 => new[] { new BattleCommandEnvelope(6, BattleCommand.CastUltimate()) },
            _ => Array.Empty<BattleCommandEnvelope>(),
        };
    }

    private static BattleEncounterSetup CreateReplaySetup()
    {
        BattleEncounterSetup setup = new();
        AddPlayer(setup, 0.4, -0.55, BattleSkillProfiles.AuraBanner);
        AddPlayer(setup, -0.1, -0.45, BattleSkillProfiles.WarDrumAura);
        AddPlayer(setup, -0.55, 0.05, BattleSkillProfiles.FrostCurseAura);
        AddPlayer(setup, -0.25, 0.55, BattleSkillProfiles.HealGrantedCleric);
        AddPlayer(setup, -0.42, -0.08, BattleSkillProfiles.LinePiercer);
        AddPlayer(setup, -0.58, 0.12, BattleSkillProfiles.ArcaneExecutioner);

        AddEnemyWave(setup, 0, 2);
        AddEnemyWave(setup, 1, 3);
        AddEnemyWave(setup, 2, 4);
        return setup;
    }

    private static void AddPlayer(BattleEncounterSetup setup, double x, double y, int skillProfileId)
    {
        if (!setup.TryAddPlayerUnit(CreateSpawn(x, y, 100, 2.0, 3.2, 10, 28, skillProfileId)))
        {
            throw new InvalidOperationException("Failed to add player unit.");
        }
    }

    private static void AddEnemyWave(BattleEncounterSetup setup, int waveIndex, int count)
    {
        BattleWaveSetup wave = setup.CreateEnemyWave();
        for (int i = 0; i < count; i++)
        {
            bool melee = i % 2 == 0;
            int profileId = ResolveEnemyProfile(waveIndex, i, melee);
            double x = 6.2 + waveIndex * 0.2 + i * 0.35;
            double y = (i - 1.5) * 0.5;
            BattleUnitSpawn spawn = CreateSpawn(
                x,
                y,
                melee ? 80 + waveIndex * 12 : 58 + waveIndex * 10,
                melee ? 1.9 : 1.35,
                melee ? 1.05 : 3.5,
                melee ? 9 + waveIndex * 2 : 7 + waveIndex * 2,
                22 + waveIndex * 4,
                profileId);
            if (!wave.TryAddUnit(spawn))
            {
                throw new InvalidOperationException("Failed to add enemy unit.");
            }
        }
    }

    private static int ResolveEnemyProfile(int waveIndex, int index, bool melee)
    {
        if (waveIndex == 0 && index == 1)
        {
            return BattleSkillProfiles.DeathSummonHunter;
        }

        if (waveIndex == 1 && index == 0)
        {
            return BattleSkillProfiles.AllyDeathGuard;
        }

        if (waveIndex == 1 && index == 1)
        {
            return BattleSkillProfiles.BurningMage;
        }

        return melee ? BattleSkillProfiles.Default : BattleSkillProfiles.NoBasicAttackStun;
    }

    private static BattleUnitSpawn CreateSpawn(
        double x,
        double y,
        double maxHp,
        double moveSpeed,
        double attackRange,
        double physicalAttack,
        double ultimateDamage,
        int skillProfileId,
        BattlePrimaryAttribute primaryAttribute = BattlePrimaryAttribute.Strength,
        Fix32? ultimateEnergyCost = null,
        Fix32? physicalArmor = null)
    {
        return new BattleUnitSpawn(
            Fix32.FromDouble(maxHp),
            Fix32.FromDouble(moveSpeed),
            Fix32.FromDouble(attackRange),
            Fix32.FromDouble(1.0),
            Fix32.FromDouble(physicalAttack),
            Fix32.FromDouble(ultimateDamage),
            Fix32.FromDouble(2.2),
            ultimateEnergyCost ?? Fix32.One,
            Fix32.FromDouble(0.25),
            Fix32.FromDouble(5.0),
            Fix32.FromRaw(3277),
            Fix32.FromDouble(8.0),
            BattleControlMode.AutoCombat,
            new FixVec2(Fix32.FromDouble(x), Fix32.FromDouble(y)),
            skillProfileId,
            BattleUnitTag.Hero,
            primaryAttribute,
            physicalArmor: physicalArmor ?? Fix32.Zero);
    }

    private static void AssertFrameEqual(BattleFrameOutput expected, BattleFrameOutput actual)
    {
        if (expected.FrameNo != actual.FrameNo ||
            expected.BattleEnded != actual.BattleEnded ||
            expected.Winner != actual.Winner ||
            expected.CurrentWaveIndex != actual.CurrentWaveIndex ||
            expected.AlivePlayerCount != actual.AlivePlayerCount ||
            expected.AliveEnemyCount != actual.AliveEnemyCount ||
            expected.StateHash != actual.StateHash ||
            expected.Events.Length != actual.Events.Length)
        {
            throw new InvalidOperationException(
                $"Frame {expected.FrameNo} mismatch: hash {expected.StateHash} vs {actual.StateHash}, events {expected.Events.Length} vs {actual.Events.Length}.");
        }

        for (int i = 0; i < expected.Events.Length; i++)
        {
            AssertEventEqual(expected.FrameNo, i, expected.Events[i], actual.Events[i]);
        }
    }

    private static void AssertEventEqual(int frameNo, int index, BattleEvent expected, BattleEvent actual)
    {
        if (expected.Type != actual.Type ||
            expected.UnitId != actual.UnitId ||
            expected.TargetUnitId != actual.TargetUnitId ||
            expected.Position.X.Raw != actual.Position.X.Raw ||
            expected.Position.Y.Raw != actual.Position.Y.Raw ||
            expected.Amount.Raw != actual.Amount.Raw ||
            expected.RemainingHp.Raw != actual.RemainingHp.Raw ||
            expected.WaveIndex != actual.WaveIndex ||
            expected.Team != actual.Team ||
            expected.BuffType != actual.BuffType)
        {
            throw new InvalidOperationException($"Event mismatch at frame {frameNo}, index {index}.");
        }
    }

    private static void AssertByteArraysEqual(byte[] expected, byte[] actual, string label)
    {
        if (expected.Length != actual.Length)
        {
            throw new InvalidOperationException($"{label} length mismatch.");
        }

        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                throw new InvalidOperationException($"{label} byte mismatch at offset {i}.");
            }
        }
    }
}
