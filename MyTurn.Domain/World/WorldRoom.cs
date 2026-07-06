namespace MyTurn.Domain;

public sealed class WorldRoom
{
    public WorldPosition Position { get; }
    public RoomType RoomType { get; }
    public int? EncounterSeed { get; }
    public bool IsVisited { get; private set; }
    public bool IsCleared { get; private set; }
    public bool IsLooted { get; private set; }

    public WorldRoom(
        WorldPosition position,
        RoomType roomType,
        int? encounterSeed = null,
        bool isVisited = false,
        bool isCleared = false,
        bool isLooted = false)
    {
        Position = position;
        RoomType = roomType;
        EncounterSeed = encounterSeed;
        IsVisited = isVisited;
        IsCleared = isCleared;
        IsLooted = isLooted;
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
