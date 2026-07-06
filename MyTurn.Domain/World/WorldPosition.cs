namespace MyTurn.Domain;

public readonly record struct WorldPosition(int X, int Y)
{
    public static WorldPosition Origin { get; } = new(0, 0);

    public WorldPosition Move(Direction direction)
    {
        return direction switch
        {
            Direction.North => this with { Y = Y + 1 },
            Direction.South => this with { Y = Y - 1 },
            Direction.West => this with { X = X - 1 },
            Direction.East => this with { X = X + 1 },
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported direction.")
        };
    }

    public int ManhattanDistanceFromOrigin => Math.Abs(X) + Math.Abs(Y);
}
