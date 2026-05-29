using System;
using System.Globalization;

namespace LeanClr.Mathematics;

public readonly struct FixVec2 : IEquatable<FixVec2>
{
    public readonly Fix32 X;
    public readonly Fix32 Y;

    public FixVec2(Fix32 x, Fix32 y)
    {
        X = x;
        Y = y;
    }

    public static FixVec2 Zero => new(Fix32.Zero, Fix32.Zero);
    public static FixVec2 One => new(Fix32.One, Fix32.One);
    public static FixVec2 Up => new(Fix32.Zero, Fix32.One);
    public static FixVec2 Down => new(Fix32.Zero, -Fix32.One);
    public static FixVec2 Left => new(-Fix32.One, Fix32.Zero);
    public static FixVec2 Right => new(Fix32.One, Fix32.Zero);

    public Fix32 SqrMagnitude => X * X + Y * Y;
    public Fix32 Magnitude => FixMath.Sqrt(SqrMagnitude);

    public FixVec2 Normalized
    {
        get
        {
            Fix32 magnitude = Magnitude;
            return magnitude <= FixMath.Epsilon ? Zero : this / magnitude;
        }
    }

    public Fix32 this[int index] => index switch
    {
        0 => X,
        1 => Y,
        _ => throw new IndexOutOfRangeException()
    };

    public static FixVec2 operator +(FixVec2 a, FixVec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static FixVec2 operator -(FixVec2 a, FixVec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static FixVec2 operator -(FixVec2 value) => new(-value.X, -value.Y);
    public static FixVec2 operator *(FixVec2 value, Fix32 scale) => new(value.X * scale, value.Y * scale);
    public static FixVec2 operator *(Fix32 scale, FixVec2 value) => value * scale;
    public static FixVec2 operator /(FixVec2 value, Fix32 scale) => new(value.X / scale, value.Y / scale);

    public static bool operator ==(FixVec2 left, FixVec2 right) => left.Equals(right);
    public static bool operator !=(FixVec2 left, FixVec2 right) => !left.Equals(right);

    public static Fix32 Dot(FixVec2 a, FixVec2 b) => a.X * b.X + a.Y * b.Y;
    public static Fix32 Cross(FixVec2 a, FixVec2 b) => a.X * b.Y - a.Y * b.X;
    public static Fix32 Distance(FixVec2 a, FixVec2 b) => (a - b).Magnitude;
    public static Fix32 DistanceSquared(FixVec2 a, FixVec2 b) => (a - b).SqrMagnitude;
    public static FixVec2 Normalize(FixVec2 value) => value.Normalized;
    public static FixVec2 Lerp(FixVec2 from, FixVec2 to, Fix32 t)
        => new(FixMath.Lerp(from.X, to.X, t), FixMath.Lerp(from.Y, to.Y, t));

    public static Fix32 Angle(FixVec2 from, FixVec2 to)
    {
        Fix32 denom = from.Magnitude * to.Magnitude;
        if (denom <= FixMath.Epsilon)
        {
            return Fix32.Zero;
        }

        Fix32 cos = Dot(from, to) / denom;
        if (cos > Fix32.One)
        {
            cos = Fix32.One;
        }
        else if (cos < -Fix32.One)
        {
            cos = -Fix32.One;
        }

        return FixMath.Atan2(FixMath.Sqrt(Fix32.One - cos * cos), cos) * FixMath.Rad2Deg;
    }

    public FixVec2 Perpendicular() => new(-Y, X);

    public bool Equals(FixVec2 other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is FixVec2 other && Equals(other);
    public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());
    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0}, {1})", X, Y);
}
