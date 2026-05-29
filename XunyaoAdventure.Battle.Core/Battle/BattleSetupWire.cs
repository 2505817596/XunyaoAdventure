using System;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static class BattleSetupWire
{
    public const int Magic = 0x31455342;
    public const int Version = 4;
    public const int HeaderSize = 32;
    public const int SpawnRecordSize = 96;

    public static byte[] EncodeSetup(BattleEncounterSetup setup)
    {
        int totalSize = HeaderSize;
        totalSize += setup.PlayerUnits.Count * SpawnRecordSize;
        for (int i = 0; i < setup.EnemyWaves.Count; i++)
        {
            totalSize += 4;
            totalSize += setup.EnemyWaves[i].Count * SpawnRecordSize;
        }

        byte[] buffer = new byte[totalSize];
        int offset = 0;

        WriteInt32(buffer, ref offset, Magic);
        WriteInt32(buffer, ref offset, Version);
        WriteInt32(buffer, ref offset, setup.PlayerUnits.Count);
        WriteInt32(buffer, ref offset, setup.EnemyWaves.Count);
        WriteInt32(buffer, ref offset, buffer.Length);
        WriteInt32(buffer, ref offset, 0);
        WriteInt32(buffer, ref offset, 0);
        WriteInt32(buffer, ref offset, 0);

        for (int i = 0; i < setup.PlayerUnits.Count; i++)
        {
            WriteSpawn(buffer, ref offset, setup.PlayerUnits[i]);
        }

        for (int waveIndex = 0; waveIndex < setup.EnemyWaves.Count; waveIndex++)
        {
            BattleWaveSetup wave = setup.EnemyWaves[waveIndex];
            WriteInt32(buffer, ref offset, wave.Count);
            for (int i = 0; i < wave.Count; i++)
            {
                WriteSpawn(buffer, ref offset, wave.Units[i]);
            }
        }

        return buffer;
    }

    public static BattleEncounterSetup DecodeSetup(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        int offset = 0;
        RequireHeader(buffer.Length, HeaderSize);
        RequireMagic(ReadInt32(buffer, ref offset));
        RequireVersion(ReadInt32(buffer, ref offset));

        int playerCount = ReadInt32(buffer, ref offset);
        int waveCount = ReadInt32(buffer, ref offset);
        int declaredSize = ReadInt32(buffer, ref offset);
        offset += 12;

        if (declaredSize != buffer.Length)
        {
            throw new InvalidOperationException("Setup batch size mismatch.");
        }

        if (playerCount < 0 || playerCount > BattleEncounterSetup.MaxPlayerUnits)
        {
            throw new InvalidOperationException("Invalid player unit count.");
        }

        if (waveCount < 0 || waveCount > BattleEncounterSetup.MaxEnemyWaves)
        {
            throw new InvalidOperationException("Invalid enemy wave count.");
        }

        BattleEncounterSetup setup = new();
        for (int i = 0; i < playerCount; i++)
        {
            RequireRemaining(offset, buffer.Length, SpawnRecordSize);
            if (!setup.TryAddPlayerUnit(ReadSpawn(buffer, ref offset)))
            {
                throw new InvalidOperationException("Failed to add player unit.");
            }
        }

        for (int waveIndex = 0; waveIndex < waveCount; waveIndex++)
        {
            int unitCount = ReadInt32(buffer, ref offset);
            if (unitCount < 0 || unitCount > BattleWaveSetup.MaxUnitsPerTeam)
            {
                throw new InvalidOperationException("Invalid enemy wave unit count.");
            }

            BattleWaveSetup wave = setup.CreateEnemyWave();
            for (int i = 0; i < unitCount; i++)
            {
                RequireRemaining(offset, buffer.Length, SpawnRecordSize);
                if (!wave.TryAddUnit(ReadSpawn(buffer, ref offset)))
                {
                    throw new InvalidOperationException("Failed to add enemy unit.");
                }
            }
        }

        if (offset != buffer.Length)
        {
            throw new InvalidOperationException("Setup wire has trailing bytes.");
        }

        return setup;
    }

    private static void WriteSpawn(byte[] buffer, ref int offset, BattleUnitSpawn spawn)
    {
        WriteInt32(buffer, ref offset, spawn.Spec.MaxHp.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.MoveSpeed.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.AttackRange.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.AttackInterval.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.PhysicalAttack.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.UltimateDamage.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.UltimateRadius.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.UltimateEnergyCost.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.EnergyRegenPerSecond.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.UltimateCooldown.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.Radius.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.AggroRange.Raw);
        WriteInt32(buffer, ref offset, (int)spawn.Spec.ControlMode);
        WriteInt32(buffer, ref offset, spawn.Position.X.Raw);
        WriteInt32(buffer, ref offset, spawn.Position.Y.Raw);
        WriteInt32(buffer, ref offset, spawn.SkillProfileId);
        WriteInt32(buffer, ref offset, (int)spawn.Spec.PrimaryAttribute);
        WriteInt32(buffer, ref offset, spawn.Spec.Level);
        WriteInt32(buffer, ref offset, spawn.Spec.MagicPower.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.PhysicalArmor.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.MagicResist.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.PhysicalCrit.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.HpRegenPerSecond.Raw);
        WriteInt32(buffer, ref offset, spawn.Spec.InterruptThreshold.Raw);
    }

    private static BattleUnitSpawn ReadSpawn(byte[] buffer, ref int offset)
    {
        RequireRemaining(offset, buffer.Length, SpawnRecordSize);
        Fix32 maxHp = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 moveSpeed = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 attackRange = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 attackInterval = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 physicalAttack = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 ultimateDamage = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 ultimateRadius = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 ultimateEnergyCost = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 energyRegenPerSecond = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 ultimateCooldown = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 radius = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 aggroRange = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        BattleControlMode controlMode = (BattleControlMode)ReadInt32(buffer, ref offset);
        FixVec2 position = new(Fix32.FromRaw(ReadInt32(buffer, ref offset)), Fix32.FromRaw(ReadInt32(buffer, ref offset)));
        int skillProfileId = ReadInt32(buffer, ref offset);
        BattlePrimaryAttribute primaryAttribute = (BattlePrimaryAttribute)ReadInt32(buffer, ref offset);
        int level = ReadInt32(buffer, ref offset);
        Fix32 magicPower = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 physicalArmor = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 magicResist = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 physicalCrit = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 hpRegenPerSecond = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        Fix32 interruptThreshold = Fix32.FromRaw(ReadInt32(buffer, ref offset));
        RequirePrimaryAttribute(primaryAttribute);
        return new BattleUnitSpawn(
            maxHp,
            moveSpeed,
            attackRange,
            attackInterval,
            physicalAttack,
            ultimateDamage,
            ultimateRadius,
            ultimateEnergyCost,
            energyRegenPerSecond,
            ultimateCooldown,
            radius,
            aggroRange,
            controlMode,
            position,
            skillProfileId,
            BattleUnitTag.Hero,
            primaryAttribute,
            level,
            magicPower,
            physicalArmor,
            magicResist,
            physicalCrit,
            hpRegenPerSecond,
            interruptThreshold);
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
            throw new InvalidOperationException("Invalid setup wire magic.");
        }
    }

    private static void RequireVersion(int version)
    {
        if (version != Version)
        {
            throw new InvalidOperationException("Unsupported setup wire version.");
        }
    }

    private static void RequirePrimaryAttribute(BattlePrimaryAttribute primaryAttribute)
    {
        if (primaryAttribute != BattlePrimaryAttribute.Strength
            && primaryAttribute != BattlePrimaryAttribute.Intelligence
            && primaryAttribute != BattlePrimaryAttribute.Agility)
        {
            throw new InvalidOperationException("Invalid primary attribute.");
        }
    }

    private static void RequireRemaining(int offset, int length, int requiredBytes)
    {
        if (offset < 0 || requiredBytes < 0 || offset > length - requiredBytes)
        {
            throw new InvalidOperationException("Setup wire buffer truncated.");
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

