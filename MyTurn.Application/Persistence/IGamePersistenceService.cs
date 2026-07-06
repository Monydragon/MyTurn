using MyTurn.Domain;

namespace MyTurn.Application;

public interface IGamePersistenceService
{
    IReadOnlyList<SaveSlotSummary> ListSaves();
    GameSession CreateSave(Party party);
    GameSession CreateSave(Actor actor);
    GameSession LoadSave(Guid saveSlotId);
    void Save(GameSession session);
}
