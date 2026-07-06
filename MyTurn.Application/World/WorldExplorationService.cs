using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class WorldExplorationService : IExplorationService
{
    private readonly IEncounterGenerator _encounterGenerator;
    private readonly ITreasureLootService _treasureLootService;

    public WorldExplorationService(IEncounterGenerator encounterGenerator, ITreasureLootService treasureLootService)
    {
        _encounterGenerator = encounterGenerator;
        _treasureLootService = treasureLootService;
    }

    public ExplorationResult TryMove(Actor actor, WorldSession session, Direction direction)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(session);

        var nextPosition = session.CurrentPosition.Move(direction);

        if (!session.Map.Contains(nextPosition))
        {
            return ExplorationResult.Blocked(session.CurrentRoom, "You cannot travel farther in that direction.");
        }

        session.MoveTo(nextPosition);
        actor.AddSteps(1);

        return EnterCurrentRoom(actor, session);
    }

    public ExplorationResult EnterCurrentRoom(Actor actor, WorldSession session)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(session);

        var room = session.CurrentRoom;

        return room.RoomType switch
        {
            RoomType.Enemy when !room.IsCleared => new ExplorationResult(
                ExplorationState.EnemyEncounter,
                room,
                _encounterGenerator.Generate(seed: room.EncounterSeed ?? WorldGenerator.DeriveRoomSeed(session.Map.Seed, room.Position)),
                LootReward.Empty,
                "An enemy group blocks the room."),
            RoomType.Treasure when !room.IsLooted => new ExplorationResult(
                ExplorationState.TreasureFound,
                room,
                null,
                _treasureLootService.ClaimTreasure(actor, session, room),
                "You found treasure."),
            RoomType.Exit => CompleteExit(session, room),
            _ => new ExplorationResult(ExplorationState.Moved, room, null, LootReward.Empty, "You enter the room.")
        };
    }

    public void ClearEnemyRoom(WorldSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CurrentRoom.RoomType == RoomType.Enemy)
        {
            session.CurrentRoom.MarkCleared();
        }
    }

    private static ExplorationResult CompleteExit(WorldSession session, WorldRoom room)
    {
        session.MarkCompleted();

        return new ExplorationResult(
            ExplorationState.ExitReached,
            room,
            null,
            LootReward.Empty,
            "You found the exit and completed this world.");
    }

}
