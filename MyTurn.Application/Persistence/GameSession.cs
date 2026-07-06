using MyTurn.Domain;

namespace MyTurn.Application;

public sealed record GameSession(Guid SaveSlotId, Party Party, WorldSession? ActiveWorldSession)
{
    public Actor Actor => Party.Leader;
}
