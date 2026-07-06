using MyTurn.Domain;

namespace MyTurn.Application;

public interface IMinimapService
{
    MinimapSnapshot CreateSnapshot(WorldSession session);
}
