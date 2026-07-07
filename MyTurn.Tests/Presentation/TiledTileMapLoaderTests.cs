using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Presentation;

namespace MyTurn.Tests.Presentation;

[TestFixture]
public sealed class TiledTileMapLoaderTests
{
    [Test]
    public void Load_ReadsPrototypeMapTilesetsCollisionAndObjects()
    {
        var map = new TiledTileMapLoader().Load(GetPrototypeMapPath());

        Assert.Multiple(() =>
        {
            Assert.That(map.Name, Is.EqualTo("prototype_dungeon"));
            Assert.That(map.Width, Is.EqualTo(13));
            Assert.That(map.Height, Is.EqualTo(11));
            Assert.That(map.TileWidth, Is.EqualTo(16));
            Assert.That(map.TileHeight, Is.EqualTo(16));
            Assert.That(map.Layers, Has.Count.EqualTo(1));
            Assert.That(map.Tilesets, Has.Count.EqualTo(1));
            Assert.That(map.SpawnTile, Is.EqualTo(new WorldPosition(2, 8)));
            Assert.That(map.FindTile(1)?.Type, Is.EqualTo("floor"));
            Assert.That(map.FindTile(2)?.IsBlocking, Is.True);
            Assert.That(map.IsBlocked(new WorldPosition(0, 0)), Is.True);
            Assert.That(map.IsBlocked(new WorldPosition(2, 8)), Is.False);
            Assert.That(map.FindObjectAt(new WorldPosition(8, 6), "enemy"), Is.Not.Null);
            Assert.That(map.FindObjectAt(new WorldPosition(4, 3), "treasure"), Is.Not.Null);
            Assert.That(map.FindObjectAt(new WorldPosition(10, 2), "exit"), Is.Not.Null);
        });
    }

    [Test]
    public void TiledWorldGenerator_CreatesDomainWorldWithStartExitAndEvents()
    {
        var mapPath = GetPrototypeMapPath();
        var loader = new TiledTileMapLoader();
        var world = new TiledWorldGenerator(loader, mapPath).Generate(new WorldGenerationRequest(8128));

        Assert.Multiple(() =>
        {
            Assert.That(world.Seed, Is.EqualTo(8128));
            Assert.That(world.GetRoom(WorldPosition.Origin).RoomType, Is.EqualTo(RoomType.Start));
            Assert.That(world.GetRoom(new WorldPosition(8, 6)).RoomType, Is.EqualTo(RoomType.Exit));
            Assert.That(world.GetRoom(new WorldPosition(6, 2)).RoomType, Is.EqualTo(RoomType.Enemy));
            Assert.That(world.GetRoom(new WorldPosition(2, 5)).RoomType, Is.EqualTo(RoomType.Treasure));
            Assert.That(world.GetRoom(new WorldPosition(6, 2)).EncounterSeed, Is.Not.Null);
        });
    }

    private static string GetPrototypeMapPath()
    {
        return Path.Combine(FindRepoRoot(), "MyTurn.Desktop", "Content", "Maps", "prototype_dungeon.tmj");
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
}
