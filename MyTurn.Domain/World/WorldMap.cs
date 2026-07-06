namespace MyTurn.Domain;

public sealed class WorldMap
{
    private readonly Dictionary<WorldPosition, WorldRoom> _rooms;

    public int Seed { get; }
    public int MinCoordinate { get; }
    public int MaxCoordinate { get; }
    public WorldPosition StartPosition { get; } = WorldPosition.Origin;
    public WorldPosition ExitPosition { get; }
    public IReadOnlyDictionary<WorldPosition, WorldRoom> Rooms => _rooms;

    public WorldMap(int seed, int minCoordinate, int maxCoordinate, IEnumerable<WorldRoom> rooms)
    {
        if (minCoordinate > maxCoordinate)
        {
            throw new ArgumentOutOfRangeException(nameof(minCoordinate), "Minimum coordinate cannot exceed maximum coordinate.");
        }

        Seed = seed;
        MinCoordinate = minCoordinate;
        MaxCoordinate = maxCoordinate;
        _rooms = rooms.ToDictionary(room => room.Position);

        if (!_rooms.TryGetValue(StartPosition, out var startRoom) || startRoom.RoomType != RoomType.Start)
        {
            throw new ArgumentException("World map must include a start room at (0, 0).", nameof(rooms));
        }

        var exitRooms = _rooms.Values.Where(room => room.RoomType == RoomType.Exit).ToArray();

        if (exitRooms.Length != 1)
        {
            throw new ArgumentException("World map must include exactly one exit room.", nameof(rooms));
        }

        ExitPosition = exitRooms[0].Position;
    }

    public bool Contains(WorldPosition position)
    {
        return position.X >= MinCoordinate
            && position.X <= MaxCoordinate
            && position.Y >= MinCoordinate
            && position.Y <= MaxCoordinate
            && _rooms.ContainsKey(position);
    }

    public WorldRoom GetRoom(WorldPosition position)
    {
        return _rooms.TryGetValue(position, out var room)
            ? room
            : throw new KeyNotFoundException($"No room exists at ({position.X}, {position.Y}).");
    }
}
