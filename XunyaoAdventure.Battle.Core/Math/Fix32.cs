using System;
using System.Globalization;

namespace LeanClr.Mathematics;

/// <summary>
/// Signed fixed-point number with 16 fractional bits.
/// </summary>
public readonly struct Fix32 : IEquatable<Fix32>, IComparable<Fix32>
{
    public const int FractionalBits = 16;
    public const int OneRaw = 1 << FractionalBits;
    public const long HalfRaw = 1L << (FractionalBits - 1);

    public readonly int Raw;

    public Fix32(int raw, bool isRaw)
    {
        Raw = raw;
    }

    public static Fix32 FromRaw(int raw) => new(raw, true);
    public static Fix32 FromInt(int value) => new(value << FractionalBits, true);
    public static Fix32 FromFloat(float value) => new((int)Math.Round(value * OneRaw, MidpointRounding.AwayFromZero), true);
    public static Fix32 FromDouble(double value) => new((int)Math.Round(value * OneRaw, MidpointRounding.AwayFromZero), true);

    public float ToFloat() => Raw / (float)OneRaw;
    public double ToDouble() => Raw / (double)OneRaw;

    public static Fix32 Zero => default;
    public static Fix32 One => FromInt(1);
    public static Fix32 Half => FromRaw(OneRaw >> 1);

    public static Fix32 operator +(Fix32 a, Fix32 b) => FromRaw(a.Raw + b.Raw);
    public static Fix32 operator -(Fix32 a, Fix32 b) => FromRaw(a.Raw - b.Raw);
    public static Fix32 operator -(Fix32 value) => FromRaw(-value.Raw);

    public static Fix32 operator *(Fix32 a, Fix32 b)
        => FromRaw((int)(((long)a.Raw * b.Raw + HalfRaw) >> FractionalBits));

    public static Fix32 operator /(Fix32 a, Fix32 b)
        => FromRaw((int)((((long)a.Raw) << FractionalBits) / b.Raw));

    public static bool operator ==(Fix32 left, Fix32 right) => left.Raw == right.Raw;
    public static bool operator !=(Fix32 left, Fix32 right) => left.Raw != right.Raw;
    public static bool operator <(Fix32 left, Fix32 right) => left.Raw < right.Raw;
    public static bool operator >(Fix32 left, Fix32 right) => left.Raw > right.Raw;
    public static bool operator <=(Fix32 left, Fix32 right) => left.Raw <= right.Raw;
    public static bool operator >=(Fix32 left, Fix32 right) => left.Raw >= right.Raw;

    public static explicit operator Fix32(int value) => FromInt(value);
    public static explicit operator Fix32(float value) => FromFloat(value);
    public static explicit operator Fix32(double value) => FromDouble(value);
    public static implicit operator double(Fix32 value) => value.ToDouble();

    public int CompareTo(Fix32 other) => Raw.CompareTo(other.Raw);

    public bool Equals(Fix32 other) => Raw == other.Raw;
    public override bool Equals(object? obj) => obj is Fix32 other && Equals(other);
    public override int GetHashCode() => Raw;

    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0:0.######}", ToFloat());

    public static Fix32 Abs(Fix32 value) => FromRaw(Math.Abs(value.Raw));
    public static Fix32 Min(Fix32 a, Fix32 b) => a.Raw <= b.Raw ? a : b;
    public static Fix32 Max(Fix32 a, Fix32 b) => a.Raw >= b.Raw ? a : b;
    public static Fix32 Clamp(Fix32 value, Fix32 min, Fix32 max) => Max(min, Min(value, max));
    public static Fix32 Lerp(Fix32 from, Fix32 to, Fix32 t) => from + (to - from) * t;
}
