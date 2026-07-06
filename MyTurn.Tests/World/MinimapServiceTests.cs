using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.World;

[TestFixture]
public sealed class MinimapServiceTests
{
    [Test]
    public void CreateSnapshot_ShowsVisitedAndAdjacentUnknownRoomsOnly()
    {
        var map = TestWorldFactory.CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(-1, -1)] = RoomType.Empty,
            [new(0, -1)] = RoomType.Empty,
            [new(1, -1)] = RoomType.Empty,
            [new(-1, 0)] = RoomType.Empty,
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Empty,
            [new(-1, 1)] = RoomType.Empty,
            [new(0, 1)] = RoomType.Enemy,
            [new(1, 1)] = RoomType.Exit
        }, min: -1, max: 1);
        var session = new WorldSession(map);
        var service = new MinimapService();

        var snapshot = service.CreateSnapshot(session);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.GetCell(WorldPosition.Origin).Symbol, Is.EqualTo('@'));
            Assert.That(snapshot.GetCell(new WorldPosition(0, 1)).Symbol, Is.EqualTo('?'));
            Assert.That(snapshot.GetCell(new WorldPosition(1, 1)).Symbol, Is.EqualTo(' '));
        });
    }
}
