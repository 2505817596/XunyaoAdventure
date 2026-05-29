using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal static class BattleHash
{
    private const uint OffsetBasis = 2166136261;
    private const uint Prime = 16777619;

    public static int Begin() => unchecked((int)OffsetBasis);

    public static int Mix(int hash, int value)
    {
        unchecked
        {
            uint result = (uint)hash;
            result ^= (uint)value;
            result *= Prime;
            return (int)result;
        }
    }

    public static int Mix(int hash, bool value) => Mix(hash, value ? 1 : 0);
    public static int Mix(int hash, Fix32 value) => Mix(hash, value.Raw);
    public static int Mix(int hash, FixVec2 value)
    {
        hash = Mix(hash, value.X);
        return Mix(hash, value.Y);
    }
}
