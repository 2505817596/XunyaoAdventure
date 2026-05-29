using System;
using System.Runtime.InteropServices;
using LeanClr.Mathematics;

namespace LeanClr.Battle;

public static class BattleRuntime
{
    private static BattleHost? _host;
    private static int _frameNo;

    public static bool IsInitialized => _host != null;

    public static int InitializeFromBridge()
    {
        int setupSize = BattleBridgeApi.GetSetupBatchSize(0);
        if (setupSize <= 0)
        {
            return -1;
        }

        byte[] setupBytes = new byte[setupSize];
        GCHandle setupHandle = GCHandle.Alloc(setupBytes, GCHandleType.Pinned);
        try
        {
            int copied = BattleBridgeApi.TakeSetupBatch(setupHandle.AddrOfPinnedObject(), setupSize);
            if (copied <= 0)
            {
                return -2;
            }

            BattleEncounterSetup setup = BattleSetupWire.DecodeSetup(setupBytes);
            _host = new BattleHost(setup);
            _host.Start();
            _frameNo = 0;
            return 0;
        }
        finally
        {
            setupHandle.Free();
        }
    }

    public static int Pump()
    {
        if (_host == null)
        {
            return -1;
        }

        int inputSize = BattleBridgeApi.GetInputBatchSize(0);
        BattleFrameInput input;
        if (inputSize <= 0)
        {
            input = new BattleFrameInput(_frameNo, Fix32.FromDouble(0.2), Array.Empty<BattleCommandEnvelope>());
        }
        else
        {
            byte[] inputBytes = new byte[inputSize];
            GCHandle inputHandle = GCHandle.Alloc(inputBytes, GCHandleType.Pinned);
            try
            {
                int copied = BattleBridgeApi.TakeInputBatch(inputHandle.AddrOfPinnedObject(), inputSize);
                if (copied <= 0)
                {
                    return -2;
                }

                input = BattleWire.DecodeCommandBatch(inputBytes);
                _frameNo = input.FrameNo;
            }
            finally
            {
                inputHandle.Free();
            }
        }

        BattleFrameOutput output = _host.Step(input);
        byte[] outputBytes = BattleWire.EncodeEventBatch(output);

        GCHandle outputHandle = GCHandle.Alloc(outputBytes, GCHandleType.Pinned);
        try
        {
            int pushResult = BattleBridgeApi.PushOutputBatch(outputHandle.AddrOfPinnedObject(), outputBytes.Length);
            if (pushResult != 0)
            {
                return pushResult;
            }
        }
        finally
        {
            outputHandle.Free();
        }

        if (inputSize <= 0)
        {
            _frameNo = input.FrameNo + 1;
        }

        return 0;
    }
}
