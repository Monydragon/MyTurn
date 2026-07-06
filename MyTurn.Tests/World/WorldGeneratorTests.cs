using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.World;

[TestFixture]
public sealed class WorldGeneratorTests
{
    [Test]
    public void Generate_WithSameSeed_ReturnsSameLayout()
    {
        var generator = new WorldGenerator();

        var first = generator.Generate(new WorldGenerationRequest(1234));
        var second = generator.Generate(new WorldGenerationRequest(1234));

        var firstLayout = first.Rooms.Values
            .OrderBy(room => room.Position.X)
            .ThenBy(room => room.Position.Y)
            .Select(room => (room.Position, room.RoomType));
        var secondLayout = second.Rooms.Values
            .OrderBy(room => room.Position.X)
            .ThenBy(room => room.Position.Y)
            .Select(room => (room.Position, room.RoomType));

        Assert.That(secondLayout, Is.EqualTo(firstLayout));
    }

    [Test]
    public void Generate_CreatesStartAtOriginAndReachableExit()
    {
        var generator = new WorldGenerator();

        var map = generator.Generate(new WorldGenerationRequest(5678));

        Assert.Multiple(() =>
        {
            Assert.That(map.GetRoom(WorldPosition.Origin).RoomType, Is.EqualTo(RoomType.Start));
            Assert.That(map.GetRoom(map.ExitPosition).RoomType, Is.EqualTo(RoomType.Exit));
            Assert.That(IsReachable(map, WorldPosition.Origin, map.ExitPosition), Is.True);
        });
    }

    private static bool IsReachable(WorldMap map, WorldPosition start, WorldPosition target)
    {
        var visited = new HashSet<WorldPosition>();
        var queue = new Queue<WorldPosition>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var position = queue.Dequeue();

            if (position == target)
            {
                return true;
            }

            foreach (var direction in Enum.GetValues<Direction>())
            {
                var next = position.Move(direction);

                if (map.Contains(next) && visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }
}
