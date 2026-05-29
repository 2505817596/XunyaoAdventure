namespace LeanClr.Mathematics;

public struct FixRandom
{
    private uint _state;

    public FixRandom(uint seed)
    {
        _state = seed != 0 ? seed : 0x6D2B79F5u;
    }

    public uint Seed
    {
        readonly get => _state;
        set => _state = value != 0 ? value : 0x6D2B79F5u;
    }

    public uint NextUInt()
    {
        uint x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;
        return x;
    }

    public int NextInt()
    {
        return (int)(NextUInt() & 0x7FFFFFFF);
    }

    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            return 0;
        }

        return (int)(NextUInt() % (uint)maxExclusive);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        uint range = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt() % range);
    }

    public Fix32 NextFix()
    {
        return Fix32.FromRaw((int)(NextUInt() & 0x7FFFFFFF));
    }

    public Fix32 NextFix(Fix32 maxExclusive)
    {
        if (maxExclusive.Raw <= 0)
        {
            return Fix32.Zero;
        }

        return Fix32.FromRaw((int)((NextUInt() % (uint)maxExclusive.Raw)));
    }

    public Fix32 NextFix(Fix32 minInclusive, Fix32 maxExclusive)
    {
        if (maxExclusive.Raw <= minInclusive.Raw)
        {
            return minInclusive;
        }

        uint range = (uint)(maxExclusive.Raw - minInclusive.Raw);
        return Fix32.FromRaw(minInclusive.Raw + (int)(NextUInt() % range));
    }

    public bool NextBool()
    {
        return (NextUInt() & 1u) != 0;
    }

    public void Skip(int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            NextUInt();
        }
    }
}
