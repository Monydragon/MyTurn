using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.World;

[TestFixture]
public sealed class ExplorationServiceTests
{
    [Test]
    public void TryMove_RejectsOutOfBoundsMove()
    {
        var actor = TestWorldFactory.CreateActor();
        var session = new WorldSession(TestWorldFactory.CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Empty,
            [new(0, 1)] = RoomType.Empty,
            [new(1, 1)] = RoomType.Exit
        }, min: 0, max: 1));
        var service = TestWorldFactory.CreateExplorationService();

        var result = service.TryMove(actor, session, Direction.West);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ExplorationState.Blocked));
            Assert.That(session.CurrentPosition, Is.EqualTo(WorldPosition.Origin));
            Assert.That(actor.Steps, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryMove_ValidMoveUpdatesPositionVisitsRoomAndAddsStep()
    {
        var actor = TestWorldFactory.CreateActor();
        var session = new WorldSession(TestWorldFactory.CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Empty,
            [new(0, 1)] = RoomType.Empty,
            [new(1, 1)] = RoomType.Exit
        }, min: 0, max: 1));
        var service = TestWorldFactory.CreateExplorationService();

        var result = service.TryMove(actor, session, Direction.East);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ExplorationState.Moved));
            Assert.That(session.CurrentPosition, Is.EqualTo(new WorldPosition(1, 0)));
            Assert.That(session.CurrentRoom.IsVisited, Is.True);
            Assert.That(actor.Steps, Is.EqualTo(1));
        });
    }

    [Test]
    public void EnemyRoom_TriggersEncounterOnceAndCanBeCleared()
    {
        var actor = TestWorldFactory.CreateActor();
        var session = new WorldSession(TestWorldFactory.CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(0, 1)] = RoomType.Enemy,
            [new(1, 0)] = RoomType.Empty,
            [new(1, 1)] = RoomType.Exit
        }, min: 0, max: 1));
        var service = TestWorldFactory.CreateExplorationService();

        var result = service.TryMove(actor, session, Direction.North);
        service.ClearEnemyRoom(session);
        var afterClear = service.EnterCurrentRoom(actor, session);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ExplorationState.EnemyEncounter));
            Assert.That(result.Encounter, Is.Not.Null);
            Assert.That(session.CurrentRoom.IsCleared, Is.True);
            Assert.That(afterClear.State, Is.EqualTo(ExplorationState.Moved));
        });
    }

    [Test]
    public void TreasureRoom_GrantsRewardsOnce()
    {
        var actor = TestWorldFactory.CreateActor();
        var session = new WorldSession(TestWorldFactory.CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(0, 1)] = RoomType.Treasure,
            [new(1, 0)] = RoomType.Empty,
            [new(1, 1)] = RoomType.Exit
        }, min: 0, max: 1));
        var service = TestWorldFactory.CreateExplorationService();

        var first = service.TryMove(actor, session, Direction.North);
        var currencyAfterFirst = actor.Inventory.Currency;
        var second = service.EnterCurrentRoom(actor, session);

        Assert.Multiple(() =>
        {
            Assert.That(first.State, Is.EqualTo(ExplorationState.TreasureFound));
            Assert.That(first.Reward.Currency, Is.GreaterThan(0));
            Assert.That(session.CurrentRoom.IsLooted, Is.True);
            Assert.That(second.Reward.Currency, Is.EqualTo(0));
            Assert.That(actor.Inventory.Currency, Is.EqualTo(currencyAfterFirst));
        });
    }

    [Test]
    public void ExitRoom_ReturnsCompletionResult()
    {
        var actor = TestWorldFactory.CreateActor();
        var session = new WorldSession(TestWorldFactory.CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Exit
        }, min: 0, max: 1));
        var service = TestWorldFactory.CreateExplorationService();

        var result = service.TryMove(actor, session, Direction.East);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(ExplorationState.ExitReached));
            Assert.That(session.IsCompleted, Is.True);
        });
    }
}
