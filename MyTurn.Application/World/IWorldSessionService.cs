using MyTurn.Domain;

namespace MyTurn.Application;

public interface IWorldSessionService
{
    bool HasActiveSession(Party party);
    WorldSession GetOrCreate(Party party, int? seed = null);
    WorldSession CreateNew(Party party, int? seed = null);
    void SetActiveSession(Party party, WorldSession session);
}
