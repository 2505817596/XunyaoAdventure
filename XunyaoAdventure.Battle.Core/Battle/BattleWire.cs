using System;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static class BattleWire
{
    public const int Magic = 0x314C5442;
    public const int Version = 10;
    public const int CommandHeaderSize = 32;
    public const int CommandRecordSize = 12;
    public const int EventHeaderSize = 48;
    public const int EventRecordSize = 40;
    public const int UnitStateRecordSize = 24;

    public static byte[] EncodeCommandBatch(BattleFrameInput input)
    {
        int count = input.Commands.Length;
        byte[] buffer = new byte[CommandHeaderSize + count * CommandRecordSize];
        int offset = 0;

        WriteInt32(buffer, ref offset, Magic);
        WriteInt32(buffer, ref offset, Version);
        WriteInt32(buffer, ref offset, input.FrameNo);
        WriteInt32(buffer, ref offset, input.DeltaTime.Raw);
        WriteInt32(buffer, ref offset, count);
        WriteInt32(buffer, ref offset, buffer.Length);
        WriteInt32(buffer, ref offset, 0);
        WriteInt32(buffer, ref offset, 0);

        for (int i = 0; i < count; i++)
        {
            BattleCommandEnvelope command = input.Commands[i];
            WriteInt32(buffer, ref offset, command.UnitId);
            WriteInt32(buffer, ref offset, (int)command.Command.Type);
            WriteInt32(buffer, ref offset, command.Command.TargetUnitId);
        }

        return buffer;
    }

    public static BattleFrameInput DecodeCommandBatch(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        int offset = 0;
        RequireHeader(buffer.Length, CommandHeaderSize);
        RequireMagic(ReadInt32(buffer, ref offset));
        RequireVersion(ReadInt32(buffer, ref offset));

        int frameNo = ReadInt32(buffer, ref offset);
        Fix32 deltaTime = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        int commandCount = ReadInt32(buffer, ref offset);
        int declaredSize = ReadInt32(buffer, ref offset);
        offset += 8;

        if (declaredSize != buffer.Length)
        {
            throw new InvalidOperationException("Command batch size mismatch.");
        }

        BattleCommandEnvelope[] commands = new BattleCommandEnvelope[commandCount];
        for (int i = 0; i < commandCount; i++)
        {
            RequireRemaining(offset, buffer.Length, CommandRecordSize);
            int unitId = ReadInt32(buffer, ref offset);
            BattleCommandType type = (BattleCommandType)ReadInt32(buffer, ref offset);
            int targetUnitId = ReadInt32(buffer, ref offset);

            BattleCommand command = type switch
            {
                BattleCommandType.CastUltimate => BattleCommand.CastUltimate(targetUnitId),
                _ => BattleCommand.None,
            };

            commands[i] = new BattleCommandEnvelope(unitId, command);
        }

        return new BattleFrameInput(frameNo, deltaTime, commands);
    }

    public static byte[] EncodeEventBatch(BattleFrameOutput output)
    {
        int count = output.Events.Length;
        byte[] buffer = new byte[
            EventHeaderSize +
            count * EventRecordSize +
            output.UnitStates.Length * UnitStateRecordSize];
        int offset = 0;

        WriteInt32(buffer, ref offset, Magic);
        WriteInt32(buffer, ref offset, Version);
        WriteInt32(buffer, ref offset, output.FrameNo);
        WriteInt32(buffer, ref offset, output.BattleEnded ? 1 : 0);
        WriteInt32(buffer, ref offset, (int)output.Winner);
        WriteInt32(buffer, ref offset, output.CurrentWaveIndex);
        WriteInt32(buffer, ref offset, count);
        WriteInt32(buffer, ref offset, buffer.Length);
        WriteInt32(buffer, ref offset, output.AlivePlayerCount);
        WriteInt32(buffer, ref offset, output.AliveEnemyCount);
        WriteInt32(buffer, ref offset, output.StateHash);
        WriteInt32(buffer, ref offset, output.UnitStates.Length);

        for (int i = 0; i < count; i++)
        {
            BattleEvent e = output.Events[i];
            WriteInt32(buffer, ref offset, (int)e.Type);
            WriteInt32(buffer, ref offset, e.UnitId);
            WriteInt32(buffer, ref offset, e.TargetUnitId);
            WriteInt32(buffer, ref offset, (int)e.Team);
            WriteInt32(buffer, ref offset, e.WaveIndex);
            WriteInt32(buffer, ref offset, e.Amount.Raw);
            WriteInt32(buffer, ref offset, e.RemainingHp.Raw);
            WriteInt32(buffer, ref offset, e.Position.X.Raw);
            WriteInt32(buffer, ref offset, e.Position.Y.Raw);
            WriteInt32(buffer, ref offset, (int)e.BuffType);
        }

        for (int i = 0; i < output.UnitStates.Length; i++)
        {
            BattleUnitFrameState state = output.UnitStates[i];
            WriteInt32(buffer, ref offset, state.UnitId);
            WriteInt32(buffer, ref offset, (int)state.Team);
            WriteInt32(buffer, ref offset, state.Hp.Raw);
            WriteInt32(buffer, ref offset, state.MaxHp.Raw);
            WriteInt32(buffer, ref offset, state.Energy.Raw);
            WriteInt32(buffer, ref offset, state.IsAlive ? 1 : 0);
        }

        return buffer;
    }

    public static BattleFrameOutput DecodeEventBatch(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        int offset = 0;
        RequireHeader(buffer.Length, EventHeaderSize);
        RequireMagic(ReadInt32(buffer, ref offset));
        RequireVersion(ReadInt32(buffer, ref offset));

        int frameNo = ReadInt32(buffer, ref offset);
        bool battleEnded = ReadInt32(buffer, ref offset) != 0;
        BattleTeam winner = (BattleTeam)ReadInt32(buffer, ref offset);
        int currentWaveIndex = ReadInt32(buffer, ref offset);
        int eventCount = ReadInt32(buffer, ref offset);
        int declaredSize = ReadInt32(buffer, ref offset);
        int alivePlayerCount = ReadInt32(buffer, ref offset);
        int aliveEnemyCount = ReadInt32(buffer, ref offset);
        int stateHash = ReadInt32(buffer, ref offset);
        int unitStateCount = ReadInt32(buffer, ref offset);

        if (declaredSize != buffer.Length)
        {
            throw new InvalidOperationException("Event batch size mismatch.");
        }

        BattleEvent[] events = new BattleEvent[eventCount];
        for (int i = 0; i < eventCount; i++)
        {
            RequireRemaining(offset, buffer.Length, EventRecordSize);
            BattleEventType type = (BattleEventType)ReadInt32(buffer, ref offset);
            int unitId = ReadInt32(buffer, ref offset);
            int targetUnitId = ReadInt32(buffer, ref offset);
            BattleTeam team = (BattleTeam)ReadInt32(buffer, ref offset);
            int waveIndex = ReadInt32(buffer, ref offset);
            Fix32 amount = Fix32.FromRaw(ReadInt32(buffer, ref offset));
            Fix32 remainingHp = Fix32.FromRaw(ReadInt32(buffer, ref offset));
            FixVec2 position = new(Fix32.FromRaw(ReadInt32(buffer, ref offset)), Fix32.FromRaw(ReadInt32(buffer, ref offset)));
            BattleBuffType buffType = (BattleBuffType)ReadInt32(buffer, ref offset);

            events[i] = new BattleEvent(type, unitId, targetUnitId, position, amount, remainingHp, waveIndex, team, buffType);
        }

        BattleUnitFrameState[] unitStates = new BattleUnitFrameState[unitStateCount];
        for (int i = 0; i < unitStateCount; i++)
        {
            RequireRemaining(offset, buffer.Length, UnitStateRecordSize);
            int unitId = ReadInt32(buffer, ref offset);
            BattleTeam team = (BattleTeam)ReadInt32(buffer, ref offset);
            Fix32 hp = Fix32.FromRaw(ReadInt32(buffer, ref offset));
            Fix32 maxHp = Fix32.FromRaw(ReadInt32(buffer, ref offset));
            Fix32 energy = Fix32.FromRaw(ReadInt32(buffer, ref offset));
            bool isAlive = ReadInt32(buffer, ref offset) != 0;

            unitStates[i] = new BattleUnitFrameState(unitId, team, hp, maxHp, energy, isAlive);
        }

        return new BattleFrameOutput(frameNo, battleEnded, winner, currentWaveIndex, alivePlayerCount, aliveEnemyCount, stateHash, events, unitStates);
    }

    private static void RequireHeader(int bufferLength, int minimumSize)
    {
        if (bufferLength < minimumSize)
        {
            throw new InvalidOperationException("Batch buffer too small.");
        }
    }

    private static void RequireMagic(int magic)
    {
        if (magic != Magic)
        {
            throw new InvalidOperationException("Invalid battle wire magic.");
        }
    }

    private static void RequireVersion(int version)
    {
        if (version != Version)
        {
            throw new InvalidOperationException("Unsupported battle wire version.");
        }
    }

    private static void RequireRemaining(int offset, int length, int requiredBytes)
    {
        if (offset < 0 || requiredBytes < 0 || offset > length - requiredBytes)
        {
            throw new InvalidOperationException("Battle wire buffer truncated.");
        }
    }

    private static void WriteInt32(byte[] buffer, ref int offset, int value)
    {
        buffer[offset++] = (byte)value;
        buffer[offset++] = (byte)(value >> 8);
        buffer[offset++] = (byte)(value >> 16);
        buffer[offset++] = (byte)(value >> 24);
    }

    private static int ReadInt32(byte[] buffer, ref int offset)
    {
        int value = buffer[offset]
            | (buffer[offset + 1] << 8)
            | (buffer[offset + 2] << 16)
            | (buffer[offset + 3] << 24);
        offset += 4;
        return value;
    }
}

