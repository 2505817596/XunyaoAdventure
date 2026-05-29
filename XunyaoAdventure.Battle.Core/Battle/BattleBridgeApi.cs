using System;
using System.Runtime.InteropServices;

namespace LeanClr.Battle;

public static class BattleBridgeApi
{
    [DllImport("__Internal", EntryPoint = "battle_bridge_get_setup_batch_size", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetSetupBatchSize(int unused);

    [DllImport("__Internal", EntryPoint = "battle_bridge_take_setup_batch", CallingConvention = CallingConvention.Cdecl)]
    public static extern int TakeSetupBatch(IntPtr destination, int capacity);

    [DllImport("__Internal", EntryPoint = "battle_bridge_get_input_batch_size", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetInputBatchSize(int unused);

    [DllImport("__Internal", EntryPoint = "battle_bridge_take_input_batch", CallingConvention = CallingConvention.Cdecl)]
    public static extern int TakeInputBatch(IntPtr destination, int capacity);

    [DllImport("__Internal", EntryPoint = "battle_bridge_push_output_batch", CallingConvention = CallingConvention.Cdecl)]
    public static extern int PushOutputBatch(IntPtr source, int length);
}

