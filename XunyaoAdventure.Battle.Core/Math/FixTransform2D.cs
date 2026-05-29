namespace LeanClr.Mathematics;

public readonly struct FixTransform2D
{
    public readonly FixVec2 Position;
    public readonly Fix32 Rotation;
    public readonly FixVec2 Scale;

    public FixTransform2D(FixVec2 position, Fix32 rotation, FixVec2 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public static FixTransform2D Identity => new(FixVec2.Zero, Fix32.Zero, FixVec2.One);

    public FixMat3x2 ToMatrix() => FixMat3x2.TRS(Position, Rotation, Scale);

    public FixVec2 TransformPoint(FixVec2 point) => ToMatrix().TransformPoint(point);
    public FixVec2 TransformVector(FixVec2 vector) => ToMatrix().TransformVector(vector);

    public FixTransform2D WithPosition(FixVec2 position) => new(position, Rotation, Scale);
    public FixTransform2D WithRotation(Fix32 rotation) => new(Position, rotation, Scale);
    public FixTransform2D WithScale(FixVec2 scale) => new(Position, Rotation, scale);
}
