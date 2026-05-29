namespace LeanClr.Mathematics;

public static class FixMath
{
    public static readonly Fix32 Pi = Fix32.FromRaw(205887);
    public static readonly Fix32 TwoPi = Fix32.FromRaw(411775);
    public static readonly Fix32 HalfPi = Fix32.FromRaw(102943);
    public static readonly Fix32 Deg2Rad = Fix32.FromRaw(1144);
    public static readonly Fix32 Rad2Deg = Fix32.FromRaw(3754936);
    public static readonly Fix32 Epsilon = Fix32.FromRaw(1);

    private static readonly Fix32[] SinQuarterTable =
    {
        Fix32.FromRaw(0), Fix32.FromRaw(1144), Fix32.FromRaw(2287), Fix32.FromRaw(3430), Fix32.FromRaw(4572),
        Fix32.FromRaw(5712), Fix32.FromRaw(6850), Fix32.FromRaw(7987), Fix32.FromRaw(9121), Fix32.FromRaw(10252),
        Fix32.FromRaw(11380), Fix32.FromRaw(12505), Fix32.FromRaw(13626), Fix32.FromRaw(14742), Fix32.FromRaw(15855),
        Fix32.FromRaw(16962), Fix32.FromRaw(18064), Fix32.FromRaw(19161), Fix32.FromRaw(20252), Fix32.FromRaw(21336),
        Fix32.FromRaw(22415), Fix32.FromRaw(23486), Fix32.FromRaw(24550), Fix32.FromRaw(25607), Fix32.FromRaw(26656),
        Fix32.FromRaw(27697), Fix32.FromRaw(28729), Fix32.FromRaw(29753), Fix32.FromRaw(30767), Fix32.FromRaw(31772),
        Fix32.FromRaw(32768), Fix32.FromRaw(33754), Fix32.FromRaw(34729), Fix32.FromRaw(35693), Fix32.FromRaw(36647),
        Fix32.FromRaw(37590), Fix32.FromRaw(38521), Fix32.FromRaw(39441), Fix32.FromRaw(40348), Fix32.FromRaw(41243),
        Fix32.FromRaw(42126), Fix32.FromRaw(42995), Fix32.FromRaw(43852), Fix32.FromRaw(44695), Fix32.FromRaw(45525),
        Fix32.FromRaw(46341), Fix32.FromRaw(47143), Fix32.FromRaw(47930), Fix32.FromRaw(48703), Fix32.FromRaw(49461),
        Fix32.FromRaw(50203), Fix32.FromRaw(50931), Fix32.FromRaw(51643), Fix32.FromRaw(52339), Fix32.FromRaw(53020),
        Fix32.FromRaw(53684), Fix32.FromRaw(54332), Fix32.FromRaw(54963), Fix32.FromRaw(55578), Fix32.FromRaw(56175),
        Fix32.FromRaw(56756), Fix32.FromRaw(57319), Fix32.FromRaw(57865), Fix32.FromRaw(58393), Fix32.FromRaw(58903),
        Fix32.FromRaw(59396), Fix32.FromRaw(59870), Fix32.FromRaw(60326), Fix32.FromRaw(60764), Fix32.FromRaw(61183),
        Fix32.FromRaw(61584), Fix32.FromRaw(61966), Fix32.FromRaw(62328), Fix32.FromRaw(62672), Fix32.FromRaw(62997),
        Fix32.FromRaw(63303), Fix32.FromRaw(63589), Fix32.FromRaw(63856), Fix32.FromRaw(64104), Fix32.FromRaw(64332),
        Fix32.FromRaw(64540), Fix32.FromRaw(64729), Fix32.FromRaw(64898), Fix32.FromRaw(65048), Fix32.FromRaw(65177),
        Fix32.FromRaw(65287), Fix32.FromRaw(65376), Fix32.FromRaw(65446), Fix32.FromRaw(65496), Fix32.FromRaw(65526),
        Fix32.FromRaw(65536)
    };

    public static Fix32 Clamp(Fix32 value, Fix32 min, Fix32 max) => Fix32.Clamp(value, min, max);
    public static Fix32 Abs(Fix32 value) => Fix32.Abs(value);
    public static Fix32 Min(Fix32 a, Fix32 b) => Fix32.Min(a, b);
    public static Fix32 Max(Fix32 a, Fix32 b) => Fix32.Max(a, b);
    public static Fix32 Lerp(Fix32 from, Fix32 to, Fix32 t) => Fix32.Lerp(from, to, t);

    public static int FloorToInt(Fix32 value) => value.Raw >> Fix32.FractionalBits;

    public static int CeilToInt(Fix32 value)
    {
        int raw = value.Raw;
        int integer = raw >> Fix32.FractionalBits;
        if ((raw & (Fix32.OneRaw - 1)) != 0 && raw > 0)
        {
            integer++;
        }

        return integer;
    }

    public static int RoundToInt(Fix32 value)
    {
        int raw = value.Raw;
        return (raw + (raw >= 0 ? (int)Fix32.HalfRaw : -(int)Fix32.HalfRaw)) >> Fix32.FractionalBits;
    }

    public static Fix32 Sqrt(Fix32 value)
    {
        if (value.Raw <= 0)
        {
            return Fix32.Zero;
        }

        long n = ((long)value.Raw) << Fix32.FractionalBits;
        long x = n;
        long y = (x + 1) >> 1;
        while (y < x)
        {
            x = y;
            y = (x + n / x) >> 1;
        }

        return Fix32.FromRaw((int)x);
    }

    public static Fix32 Sin(Fix32 radians)
    {
        int raw = NormalizeAngle(radians).Raw;
        int quadrant = raw / HalfPi.Raw;
        int offset = raw % HalfPi.Raw;
        Fix32 value = SampleQuarter(offset);

        switch (quadrant)
        {
            case 0: return value;
            case 1: return SampleQuarter(HalfPi.Raw - offset);
            case 2: return -value;
            default: return -SampleQuarter(HalfPi.Raw - offset);
        }
    }

    public static Fix32 Cos(Fix32 radians) => Sin(HalfPi - radians);

    public static Fix32 Tan(Fix32 radians)
    {
        Fix32 cos = Cos(radians);
        return cos.Raw == 0 ? Fix32.Zero : Sin(radians) / cos;
    }

    public static Fix32 Atan2(Fix32 y, Fix32 x)
    {
        if (x.Raw == 0 && y.Raw == 0)
        {
            return Fix32.Zero;
        }

        if (x.Raw == 0)
        {
            return y.Raw > 0 ? HalfPi : -HalfPi;
        }

        if (x.Raw > 0)
        {
            return Atan(y / x);
        }

        return y.Raw >= 0 ? Atan(y / x) + Pi : Atan(y / x) - Pi;
    }

    public static Fix32 WrapAngle(Fix32 radians) => NormalizeAngle(radians);

    private static Fix32 NormalizeAngle(Fix32 radians)
    {
        long raw = radians.Raw % TwoPi.Raw;
        if (raw < 0)
        {
            raw += TwoPi.Raw;
        }

        return Fix32.FromRaw((int)raw);
    }

    private static Fix32 SampleQuarter(int rawOffset)
    {
        if (rawOffset <= 0)
        {
            return SinQuarterTable[0];
        }

        if (rawOffset >= HalfPi.Raw)
        {
            return SinQuarterTable[SinQuarterTable.Length - 1];
        }

        long scaled = (long)rawOffset * (SinQuarterTable.Length - 1);
        int index = (int)(scaled / HalfPi.Raw);
        int next = index + 1;
        if (next >= SinQuarterTable.Length)
        {
            return SinQuarterTable[index];
        }

        int local = (int)(scaled % HalfPi.Raw);
        Fix32 a = SinQuarterTable[index];
        Fix32 b = SinQuarterTable[next];
        Fix32 t = Fix32.FromRaw((int)(((long)local << Fix32.FractionalBits) / HalfPi.Raw));
        return a + (b - a) * t;
    }

    private static Fix32 Atan(Fix32 value)
    {
        Fix32 absValue = Abs(value);
        if (absValue > Fix32.One)
        {
            Fix32 reciprocal = Fix32.One / value;
            Fix32 atan = AtanApprox(reciprocal);
            return value.Raw > 0 ? HalfPi - atan : -HalfPi - atan;
        }

        return AtanApprox(value);
    }

    private static Fix32 AtanApprox(Fix32 value)
    {
        Fix32 absValue = Abs(value);
        Fix32 coeff = Fix32.FromRaw(18350); // 0.28
        return value / (Fix32.One + coeff * absValue * absValue);
    }
}
