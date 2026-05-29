using System.Runtime.InteropServices;
using LeanClr.Battle;
using LeanClr.Mathematics;

public static class App
{
    public static void Main()
    {
        BattleRuntimeHost.Log("小鸟");
        RunBattleMain();
    }

    public static int SelfTestMain()
    {
        if (IsBattleSnapshotWriteRequested())
        {
            string snapshotPath = BattleSkillCapabilitySnapshot.WriteGoldenFile();
            Console.WriteLine("Battle skill capability snapshot written: " + snapshotPath);
            return 0;
        }

        if (IsBattleSnapshotRequested())
        {
            Console.Write(BattleSkillCapabilitySnapshot.Build());
            return 0;
        }

        if (IsBattleSelfTestRequested())
        {
            return BattleRegressionTests.RunAll();
        }

        return RunBattleMain();
    }

    private static int RunBattleMain()
    {
        try
        {
            BattleConfigValidationResult validation = BattleConfigValidator.ValidateAll();
            if (!validation.IsValid)
            {
                BattleRuntimeHost.Log("Battle config validation failed: " + validation.Message);
                return 1;
            }

            if (BattleRuntime.InitializeFromBridge() != 0)
            {
                BattleRuntimeHost.Log("BattleRuntime initialization failed.");
                return 1;
            }
        }
        catch (System.Exception ex)
        {
            BattleRuntimeHost.Log("BattleRuntime initialization exception: " + ex);
            return 1;
        }

        return 0;
    }

    private static bool IsBattleSelfTestRequested()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--battle-self-test", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBattleSnapshotRequested()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--battle-snapshot", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBattleSnapshotWriteRequested()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--battle-snapshot-write", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static void Tick()
    {
        try
        {
            if (BattleRuntime.Pump() != 0)
            {
                BattleRuntimeHost.Log("BattleRuntime pump failed.");
                return;
            }
        }
        catch (System.Exception ex)
        {
            BattleRuntimeHost.Log("BattleRuntime pump exception: " + ex);
        }
    }

    private static string FormatEvent(BattleEvent e)
    {
        return e.Type switch
        {
            BattleEventType.BattleStarted => "Battle started",
            BattleEventType.WaveStarted => $"Wave {e.WaveIndex} started",
            BattleEventType.WaveEnded => $"Wave {e.WaveIndex} ended",
            BattleEventType.UnitSpawned => $"Spawn unit {e.UnitId} team={e.Team} pos={e.Position}",
            BattleEventType.UnitMoved => $"Move unit {e.UnitId} team={e.Team} pos={e.Position}",
            BattleEventType.UnitAttack => $"Attack unit {e.UnitId} -> {e.TargetUnitId} dmg={e.Amount}",
            BattleEventType.UnitDamaged => $"Damaged unit {e.UnitId} by {e.TargetUnitId} dmg={e.Amount} hp={e.RemainingHp}",
            BattleEventType.UnitDied => $"Died unit {e.UnitId} killedBy={e.TargetUnitId} team={e.Team}",
            BattleEventType.UltimateCast => $"Ultimate cast unit {e.UnitId} target={e.TargetUnitId} dmg={e.Amount}",
            BattleEventType.UltimateRejected => $"Ultimate rejected unit {e.UnitId} target={e.TargetUnitId}",
            BattleEventType.BattleEnded => $"Battle ended winner={e.Team}",
            _ => e.Type.ToString(),
        };
    }
}

public static class SelfTestProgram
{
    public static int Main()
        => App.SelfTestMain();
}

public static class BattleRuntimeHost
{
    [DllImport("__Internal", EntryPoint = "laya_log", CallingConvention = CallingConvention.Cdecl)]
    private static extern void LogNative(string message);

    public static void Log(string message)
    {
        LogNative(message ?? string.Empty);
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class UnionAttribute : Attribute;

    public interface IUnion
    {
        object? Value { get; }
    }
}
