using MyTurn.Domain;

namespace MyTurn.Application;

public interface IExplorationService
{
    ExplorationResult TryMove(Party party, WorldSession session, Direction direction);
    ExplorationResult EnterCurrentRoom(Party party, WorldSession session);
    ExplorationResult TryMove(Actor actor, WorldSession session, Direction direction);
    ExplorationResult EnterCurrentRoom(Actor actor, WorldSession session);
    void ClearEnemyRoom(WorldSession session);
}
