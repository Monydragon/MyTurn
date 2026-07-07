namespace MyTurn.Domain;

public sealed class WorldSession
{
    private readonly List<WorldObject> _objects;

    public Guid Id { get; }
    public WorldMap Map { get; }
    public WorldPosition CurrentPosition { get; private set; }
    public bool IsCompleted { get; private set; }
    public string? LayoutId { get; }
    public string? ProfileId { get; }
    public string? LayoutSource { get; }
    public IReadOnlyList<WorldObject> Objects => _objects;

    public WorldSession(
        WorldMap map,
        WorldPosition? currentPosition = null,
        bool isCompleted = false,
        Guid? id = null,
        string? layoutId = null,
        string? profileId = null,
        string? layoutSource = null,
        IEnumerable<WorldObject>? objects = null)
    {
        Id = id ?? Guid.NewGuid();
        Map = map ?? throw new ArgumentNullException(nameof(map));
        CurrentPosition = currentPosition ?? map.StartPosition;
        IsCompleted = isCompleted;
        LayoutId = layoutId;
        ProfileId = profileId;
        LayoutSource = layoutSource;
        _objects = objects?.ToList() ?? [];
        Map.GetRoom(CurrentPosition).MarkVisited();
    }

    public WorldRoom CurrentRoom => Map.GetRoom(CurrentPosition);
    public IReadOnlyList<WorldObject> ActiveObjectsAt(WorldPosition position)
    {
        return _objects
            .Where(worldObject => worldObject.Position == position && worldObject.IsActive)
            .ToArray();
    }

    public WorldObject? BlockingObjectAt(WorldPosition position)
    {
        return _objects.FirstOrDefault(worldObject => worldObject.Position == position && worldObject.BlocksMovement);
    }

    public WorldObject? FindObject(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return _objects.FirstOrDefault(worldObject => worldObject.Id == id);
    }

    public void MoveTo(WorldPosition position)
    {
        if (!Map.Contains(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position is outside the world map.");
        }

        CurrentPosition = position;
        CurrentRoom.MarkVisited();
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }
}
