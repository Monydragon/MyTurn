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
        var party = new Party([actor], inventory: actor.Inventory, steps: actor.Steps);

        return TryMove(party, session, direction);
    }

    public ExplorationResult TryMove(Party party, WorldSession session, Direction direction)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(session);

        var nextPosition = session.CurrentPosition.Move(direction);

        if (!session.Map.Contains(nextPosition))
        {
            return ExplorationResult.Blocked(session.CurrentRoom, "You cannot travel farther in that direction.");
        }

        var blockingObject = session.BlockingObjectAt(nextPosition);

        if (blockingObject is not null)
        {
            return ExplorationResult.Blocked(session.CurrentRoom, $"{blockingObject.ObjectType.GetDisplayName()} blocks the way.");
        }

        session.MoveTo(nextPosition);
        party.AddSteps(1);

        if (session.Objects.Count > 0)
        {
            return new ExplorationResult(ExplorationState.Moved, session.CurrentRoom, null, LootReward.Empty, "You move.");
        }

        return EnterCurrentRoom(party, session);
    }

    public ExplorationResult EnterCurrentRoom(Actor actor, WorldSession session)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return EnterCurrentRoom(new Party([actor], inventory: actor.Inventory, steps: actor.Steps), session);
    }

    public ExplorationResult EnterCurrentRoom(Party party, WorldSession session)
    {
        ArgumentNullException.ThrowIfNull(party);
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
                _treasureLootService.ClaimTreasure(party, session, room),
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
