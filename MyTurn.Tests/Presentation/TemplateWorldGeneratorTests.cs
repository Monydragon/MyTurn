using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Presentation;

namespace MyTurn.Tests.Presentation;

[TestFixture]
public sealed class TemplateWorldGeneratorTests
{
    [Test]
    public void GenerateLayout_WithSameSeed_ReturnsSameLayoutAndObjects()
    {
        var generator = CreateGenerator();

        var first = generator.GenerateLayout(new WorldGenerationRequest(2468));
        var second = generator.GenerateLayout(new WorldGenerationRequest(2468));

        var firstRooms = first.Map.Rooms.Values
            .OrderBy(room => room.Position.X)
            .ThenBy(room => room.Position.Y)
            .Select(room => (room.Position, room.RoomType));
        var secondRooms = second.Map.Rooms.Values
            .OrderBy(room => room.Position.X)
            .ThenBy(room => room.Position.Y)
            .Select(room => (room.Position, room.RoomType));
        var firstObjects = first.Objects.Select(obj => (obj.Id, obj.ObjectType, obj.Position, obj.IsBlocking));
        var secondObjects = second.Objects.Select(obj => (obj.Id, obj.ObjectType, obj.Position, obj.IsBlocking));

        Assert.Multiple(() =>
        {
            Assert.That(second.LayoutId, Is.EqualTo(first.LayoutId));
            Assert.That(secondRooms, Is.EqualTo(firstRooms));
            Assert.That(secondObjects, Is.EqualTo(firstObjects));
        });
    }

    [Test]
    public void GenerateLayout_CreatesReachableExitAndValidObjects()
    {
        var layout = CreateGenerator().GenerateLayout(new WorldGenerationRequest(1357));

        Assert.Multiple(() =>
        {
            Assert.That(layout.Map.GetRoom(WorldPosition.Origin).RoomType, Is.EqualTo(RoomType.Start));
            Assert.That(layout.Map.GetRoom(layout.Map.ExitPosition).RoomType, Is.EqualTo(RoomType.Exit));
            Assert.That(IsReachable(layout.Map, WorldPosition.Origin, layout.Map.ExitPosition), Is.True);
            Assert.That(layout.Objects, Has.Some.Matches<WorldObject>(obj => obj.ObjectType == WorldObjectType.Enemy));
            Assert.That(layout.Objects, Has.Some.Matches<WorldObject>(obj => obj.ObjectType is WorldObjectType.Treasure or WorldObjectType.Key or WorldObjectType.LockedDoor or WorldObjectType.Hazard));
            Assert.That(layout.Objects, Has.All.Matches<WorldObject>(obj => layout.Map.Contains(obj.Position)));
        });
    }

    [Test]
    public void GetTileMap_RegeneratesTileMapForPersistedSession()
    {
        var generator = CreateGenerator();
        var layout = generator.GenerateLayout(new WorldGenerationRequest(9999));
        var loadedSession = new WorldSession(
            layout.Map,
            layout.Map.StartPosition,
            layoutId: layout.LayoutId,
            profileId: layout.ProfileId,
            layoutSource: layout.LayoutSource,
            objects: layout.Objects);

        var tileMap = generator.GetTileMap(loadedSession);

        Assert.Multiple(() =>
        {
            Assert.That(tileMap, Is.Not.Null);
            Assert.That(tileMap!.Width, Is.EqualTo(36));
            Assert.That(tileMap.Height, Is.EqualTo(28));
        });
    }

    private static TemplateWorldGenerator CreateGenerator()
    {
        var profile = MapGenerationProfile.Load(GetProfilePath());
        var loader = new TiledRoomTemplateLoader(new TiledTileMapLoader());
        var catalog = loader.Load(profile);

        return new TemplateWorldGenerator(profile, catalog);
    }

    private static string GetProfilePath()
    {
        return Path.Combine(FindRepoRoot(), "MyTurn.Desktop", "Content", "Generation", "prototype_dungeon_profile.json");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MyTurn.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not find the MyTurn repository root.");
    }

    private static bool IsReachable(WorldMap map, WorldPosition start, WorldPosition target)
    {
        var visited = new HashSet<WorldPosition> { start };
        var queue = new Queue<WorldPosition>();
        queue.Enqueue(start);

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
