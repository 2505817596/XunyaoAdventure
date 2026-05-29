using System;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

public readonly struct BattleCommandEnvelope
{
    public readonly int UnitId;
    public readonly BattleCommand Command;

    public BattleCommandEnvelope(int unitId, BattleCommand command)
    {
        UnitId = unitId;
        Command = command;
    }
}

public sealed class BattleFrameInput
{
    public BattleFrameInput(int frameNo, Fix32 deltaTime, BattleCommandEnvelope[] commands)
    {
        FrameNo = frameNo;
        DeltaTime = deltaTime;
        Commands = commands ?? Array.Empty<BattleCommandEnvelope>();
    }

    public int FrameNo { get; }
    public Fix32 DeltaTime { get; }
    public BattleCommandEnvelope[] Commands { get; }
}

public sealed class BattleFrameOutput
{
    public BattleFrameOutput(
        int frameNo,
        bool battleEnded,
        BattleTeam winner,
        int currentWaveIndex,
        int alivePlayerCount,
        int aliveEnemyCount,
        int stateHash,
        BattleEvent[] events,
        BattleUnitFrameState[] unitStates)
    {
        FrameNo = frameNo;
        BattleEnded = battleEnded;
        Winner = winner;
        CurrentWaveIndex = currentWaveIndex;
        AlivePlayerCount = alivePlayerCount;
        AliveEnemyCount = aliveEnemyCount;
        StateHash = stateHash;
        Events = events ?? Array.Empty<BattleEvent>();
        UnitStates = unitStates ?? Array.Empty<BattleUnitFrameState>();
    }

    public int FrameNo { get; }
    public bool BattleEnded { get; }
    public BattleTeam Winner { get; }
    public int CurrentWaveIndex { get; }
    public int AlivePlayerCount { get; }
    public int AliveEnemyCount { get; }
    public int StateHash { get; }
    public BattleEvent[] Events { get; }
    public BattleUnitFrameState[] UnitStates { get; }

    internal static BattleFrameOutput FromWorld(int frameNo, BattleWorld world)
    {
        BattleEvent[] events = new BattleEvent[world.Events.Count];
        for (int i = 0; i < events.Length; i++)
        {
            events[i] = world.Events[i];
        }

        int stateHash = BattleHash.Begin();
        stateHash = BattleHash.Mix(stateHash, world.IsBattleEnded);
        stateHash = BattleHash.Mix(stateHash, (int)world.Winner);
        stateHash = BattleHash.Mix(stateHash, world.CurrentWaveIndex);
        stateHash = BattleHash.Mix(stateHash, world.GetAliveUnitCount(BattleTeam.TeamA));
        stateHash = BattleHash.Mix(stateHash, world.GetAliveUnitCount(BattleTeam.TeamB));
        stateHash = BattleHash.Mix(stateHash, unchecked((int)world.RandomState));
        foreach (var kv in world.Units)
        {
            BattleUnit unit = kv.Value;
            stateHash = BattleHash.Mix(stateHash, unit.Id);
            stateHash = BattleHash.Mix(stateHash, (int)unit.Team);
            stateHash = BattleHash.Mix(stateHash, unit.IsAlive);
            stateHash = BattleHash.Mix(stateHash, unit.Position);
            stateHash = BattleHash.Mix(stateHash, unit.Hp);
            stateHash = BattleHash.Mix(stateHash, unit.Energy);
            stateHash = BattleHash.Mix(stateHash, unit.MoveSpeed);
            stateHash = BattleHash.Mix(stateHash, unit.PhysicalAttack);
            stateHash = BattleHash.Mix(stateHash, unit.AttackSpeed);
            stateHash = BattleHash.Mix(stateHash, unit.PhysicalArmor);
            stateHash = BattleHash.Mix(stateHash, unit.MagicResist);
            stateHash = BattleHash.Mix(stateHash, unit.EnergyRegen);
            stateHash = BattleHash.Mix(stateHash, unit.CurrentTargetId);
            stateHash = BattleHash.Mix(stateHash, unit.AttackCooldown);
            stateHash = BattleHash.Mix(stateHash, unit.UltimateCooldown);
            stateHash = BattleHash.Mix(stateHash, (int)unit.ActionState);
            stateHash = BattleHash.Mix(stateHash, (int)unit.LastFinishedSkillType);
            stateHash = BattleHash.Mix(stateHash, unit.SkillProfileId);
            stateHash = BattleHash.Mix(stateHash, (int)unit.UnitTags);
            stateHash = BattleHash.Mix(stateHash, (int)unit.Spec.PrimaryAttribute);
        }

        for (int i = 0; i < world.Projectiles.Count; i++)
        {
            BattleProjectile projectile = world.Projectiles[i];
            stateHash = BattleHash.Mix(stateHash, projectile.Id);
            stateHash = BattleHash.Mix(stateHash, projectile.SourceUnitId);
            stateHash = BattleHash.Mix(stateHash, projectile.TargetUnitId);
            stateHash = BattleHash.Mix(stateHash, (int)projectile.Team);
            stateHash = BattleHash.Mix(stateHash, projectile.Position);
            stateHash = BattleHash.Mix(stateHash, projectile.Destination);
            stateHash = BattleHash.Mix(stateHash, projectile.Speed);
            stateHash = BattleHash.Mix(stateHash, (int)projectile.MotionType);
            stateHash = BattleHash.Mix(stateHash, (int)projectile.ImpactType);
        }

        BattleUnitFrameState[] unitStates = new BattleUnitFrameState[world.Units.Count];
        int unitStateIndex = 0;
        foreach (var kv in world.Units)
        {
            BattleUnit unit = kv.Value;
            unitStates[unitStateIndex++] = new BattleUnitFrameState(
                unit.Id,
                unit.Team,
                unit.Hp,
                unit.Spec.MaxHp,
                unit.Energy,
                unit.IsAlive);
        }

        return new BattleFrameOutput(
            frameNo,
            world.IsBattleEnded,
            world.Winner,
            world.CurrentWaveIndex,
            world.GetAliveUnitCount(BattleTeam.TeamA),
            world.GetAliveUnitCount(BattleTeam.TeamB),
            stateHash,
            events,
            unitStates);
    }
}

public readonly struct BattleUnitFrameState
{
    public readonly int UnitId;
    public readonly BattleTeam Team;
    public readonly Fix32 Hp;
    public readonly Fix32 MaxHp;
    public readonly Fix32 Energy;
    public readonly bool IsAlive;

    public BattleUnitFrameState(
        int unitId,
        BattleTeam team,
        Fix32 hp,
        Fix32 maxHp,
        Fix32 energy,
        bool isAlive)
    {
        UnitId = unitId;
        Team = team;
        Hp = hp;
        MaxHp = maxHp;
        Energy = energy;
        IsAlive = isAlive;
    }
}

public sealed class BattleHost
{
    private readonly BattleWorld _world;

    public BattleHost(BattleEncounterSetup setup)
    {
        _world = new BattleWorld(setup);
    }

    public BattleWorld World => _world;

    public void Start()
    {
        _world.StartBattle();
    }

    public BattleFrameOutput Step(BattleFrameInput input)
    {
        if (!_world.IsBattleStarted)
        {
            _world.StartBattle();
        }

        if (input.Commands.Length > 0)
        {
            for (int i = 0; i < input.Commands.Length; i++)
            {
                BattleCommandEnvelope command = input.Commands[i];
                _world.IssueCommand(command.UnitId, command.Command);
            }
        }

        _world.Tick(input.DeltaTime);
        BattleFrameOutput output = BattleFrameOutput.FromWorld(input.FrameNo, _world);
        _world.ClearEvents();
        return output;
    }
}


