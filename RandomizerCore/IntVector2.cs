namespace Z2Randomizer.RandomizerCore;

public readonly struct IntVector2
{
    public readonly int X;
    public readonly int Y;

    public IntVector2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static IntVector2 operator +(IntVector2 a, IntVector2 b)
        => new(a.X + b.X, a.Y + b.Y);

    public static IntVector2 operator -(IntVector2 a, IntVector2 b)
        => new(a.X - b.X, a.Y - b.Y);

    public static IntVector2 operator -(IntVector2 v)
        => new(-v.X, -v.Y);

    public static IntVector2 operator *(int k, IntVector2 v)
        => new(k * v.X, k * v.Y);

    public static readonly IntVector2 WEST = new(-1, 0);
    public static readonly IntVector2 EAST = new(1, 0);
    public static readonly IntVector2 NORTH = new(0, -1);
    public static readonly IntVector2 SOUTH = new(0, 1);
    public static readonly IntVector2[] CARDINALS = [NORTH, SOUTH, EAST, WEST];
}

public static class IntVector2Ext
{
    public static IntVector2 Perpendicular(this IntVector2 v)
    {
        return new(v.Y, -v.X);
    }

    public static IntVector2 PerpendicularCounterClockwise(this IntVector2 v)
    {
        return new(-v.Y, v.X);
    }
}
