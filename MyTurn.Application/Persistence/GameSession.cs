using MyTurn.Domain;

namespace MyTurn.Application;

public sealed record GameSession(Guid SaveSlotId, Actor Actor, WorldSession? ActiveWorldSession);
