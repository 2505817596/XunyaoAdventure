namespace LeanClr.Mathematics;

/// <summary>
/// 2D affine transform:
/// [ M11 M12 TX ]
/// [ M21 M22 TY ]
/// [  0   0  1  ]
/// </summary>
public readonly struct FixMat3x2
{
    public readonly Fix32 M11;
    public readonly Fix32 M12;
    public readonly Fix32 M21;
    public readonly Fix32 M22;
    public readonly Fix32 TX;
    public readonly Fix32 TY;

    public FixMat3x2(Fix32 m11, Fix32 m12, Fix32 m21, Fix32 m22, Fix32 tx, Fix32 ty)
    {
        M11 = m11;
        M12 = m12;
        M21 = m21;
        M22 = m22;
        TX = tx;
        TY = ty;
    }

    public static FixMat3x2 Identity => new(Fix32.One, Fix32.Zero, Fix32.Zero, Fix32.One, Fix32.Zero, Fix32.Zero);

    public static FixMat3x2 Translate(FixVec2 position)
        => new(Fix32.One, Fix32.Zero, Fix32.Zero, Fix32.One, position.X, position.Y);

    public static FixMat3x2 Scale(FixVec2 scale)
        => new(scale.X, Fix32.Zero, Fix32.Zero, scale.Y, Fix32.Zero, Fix32.Zero);

    public static FixMat3x2 Rotate(Fix32 radians)
    {
        Fix32 sin = FixMath.Sin(radians);
        Fix32 cos = FixMath.Cos(radians);
        return new(cos, -sin, sin, cos, Fix32.Zero, Fix32.Zero);
    }

    public static FixMat3x2 TRS(FixVec2 position, Fix32 radians, FixVec2 scale)
    {
        Fix32 sin = FixMath.Sin(radians);
        Fix32 cos = FixMath.Cos(radians);
        return new(
            cos * scale.X,
            -sin * scale.Y,
            sin * scale.X,
            cos * scale.Y,
            position.X,
            position.Y);
    }

    public FixVec2 TransformPoint(FixVec2 point)
        => new(
            point.X * M11 + point.Y * M21 + TX,
            point.X * M12 + point.Y * M22 + TY);

    public FixVec2 TransformVector(FixVec2 vector)
        => new(
            vector.X * M11 + vector.Y * M21,
            vector.X * M12 + vector.Y * M22);

    public static FixMat3x2 operator *(FixMat3x2 a, FixMat3x2 b)
    {
        return new FixMat3x2(
            a.M11 * b.M11 + a.M12 * b.M21,
            a.M11 * b.M12 + a.M12 * b.M22,
            a.M21 * b.M11 + a.M22 * b.M21,
            a.M21 * b.M12 + a.M22 * b.M22,
            a.TX * b.M11 + a.TY * b.M21 + b.TX,
            a.TX * b.M12 + a.TY * b.M22 + b.TY);
    }
}
