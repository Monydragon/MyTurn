using MyTurn.Domain;

namespace MyTurn.Application;

public interface IWorldSessionService
{
    bool HasActiveSession(Actor actor);
    WorldSession GetOrCreate(Actor actor, int? seed = null);
    WorldSession CreateNew(Actor actor, int? seed = null);
}
