namespace LeanClr.Mathematics;

public readonly struct FixRect
{
    public readonly Fix32 X;
    public readonly Fix32 Y;
    public readonly Fix32 Width;
    public readonly Fix32 Height;

    public FixRect(Fix32 x, Fix32 y, Fix32 width, Fix32 height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public Fix32 Left => X;
    public Fix32 Right => X + Width;
    public Fix32 Top => Y;
    public Fix32 Bottom => Y + Height;
    public FixVec2 Position => new(X, Y);
    public FixVec2 Size => new(Width, Height);
    public FixVec2 Center => new(X + Width * Fix32.Half, Y + Height * Fix32.Half);

    public bool Contains(FixVec2 point)
        => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    public bool Intersects(FixRect other)
        => Left <= other.Right && Right >= other.Left && Top <= other.Bottom && Bottom >= other.Top;

    public FixRect Expanded(Fix32 amount)
        => new(X - amount, Y - amount, Width + amount + amount, Height + amount + amount);

    public FixRect Offset(FixVec2 delta)
        => new(X + delta.X, Y + delta.Y, Width, Height);
}
