namespace MyTurn.Domain;

public sealed class WorldSession
{
    public Guid Id { get; }
    public WorldMap Map { get; }
    public WorldPosition CurrentPosition { get; private set; }
    public bool IsCompleted { get; private set; }

    public WorldSession(
        WorldMap map,
        WorldPosition? currentPosition = null,
        bool isCompleted = false,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Map = map ?? throw new ArgumentNullException(nameof(map));
        CurrentPosition = currentPosition ?? map.StartPosition;
        IsCompleted = isCompleted;
        Map.GetRoom(CurrentPosition).MarkVisited();
    }

    public WorldRoom CurrentRoom => Map.GetRoom(CurrentPosition);

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
