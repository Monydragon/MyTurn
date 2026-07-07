namespace MyTurn.Domain;

public sealed class WorldObject
{
    public WorldObject(
        string id,
        WorldObjectType objectType,
        WorldPosition position,
        bool isBlocking = false,
        WorldObjectState state = WorldObjectState.Active,
        int? encounterSeed = null,
        string payloadJson = "")
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("World object id is required.", nameof(id));
        }

        Id = id;
        ObjectType = objectType;
        Position = position;
        IsBlocking = isBlocking;
        State = state;
        EncounterSeed = encounterSeed;
        PayloadJson = payloadJson ?? string.Empty;
    }

    public string Id { get; }
    public WorldObjectType ObjectType { get; }
    public WorldPosition Position { get; }
    public bool IsBlocking { get; private set; }
    public WorldObjectState State { get; private set; }
    public int? EncounterSeed { get; }
    public string PayloadJson { get; }

    public bool IsActive => State == WorldObjectState.Active;
    public bool BlocksMovement => IsActive && IsBlocking;

    public void MarkCollected()
    {
        State = WorldObjectState.Collected;
        IsBlocking = false;
    }

    public void MarkOpened()
    {
        State = WorldObjectState.Opened;
        IsBlocking = false;
    }

    public void MarkTriggered()
    {
        State = WorldObjectState.Triggered;
        IsBlocking = false;
    }

    public void MarkCleared()
    {
        State = WorldObjectState.Cleared;
        IsBlocking = false;
    }
}
