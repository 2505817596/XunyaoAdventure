namespace LeanClr.Mathematics;

public readonly struct FixAabb2
{
    public readonly FixVec2 Min;
    public readonly FixVec2 Max;

    public FixAabb2(FixVec2 min, FixVec2 max)
    {
        Min = min;
        Max = max;
    }

    public static FixAabb2 FromRect(FixRect rect)
        => new(rect.Position, rect.Position + rect.Size);

    public FixVec2 Center => (Min + Max) * Fix32.Half;
    public FixVec2 Size => Max - Min;

    public bool Contains(FixVec2 point)
        => point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y;

    public bool Intersects(FixAabb2 other)
        => Min.X <= other.Max.X && Max.X >= other.Min.X && Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;

    public FixAabb2 Encapsulate(FixVec2 point)
        => new(
            new FixVec2(FixMath.Min(Min.X, point.X), FixMath.Min(Min.Y, point.Y)),
            new FixVec2(FixMath.Max(Max.X, point.X), FixMath.Max(Max.Y, point.Y)));
}
