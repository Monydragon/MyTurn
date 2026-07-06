namespace MyTurn.Domain;

public sealed class WorldRoom
{
    public WorldPosition Position { get; }
    public RoomType RoomType { get; }
    public bool IsVisited { get; private set; }
    public bool IsCleared { get; private set; }
    public bool IsLooted { get; private set; }

    public WorldRoom(WorldPosition position, RoomType roomType)
    {
        Position = position;
        RoomType = roomType;
    }

    public void MarkVisited()
    {
        IsVisited = true;
    }

    public void MarkCleared()
    {
        IsCleared = true;
    }

    public void MarkLooted()
    {
        IsLooted = true;
    }
}
