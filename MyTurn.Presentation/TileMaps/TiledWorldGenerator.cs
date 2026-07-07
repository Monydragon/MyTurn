using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Presentation;

public sealed class TiledWorldGenerator : IWorldGenerator
{
    private readonly ITileMapLoader _loader;
    private readonly string _mapPath;

    public TiledWorldGenerator(ITileMapLoader loader, string mapPath)
    {
        _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        _mapPath = !string.IsNullOrWhiteSpace(mapPath)
            ? mapPath
            : throw new ArgumentException("Map path is required.", nameof(mapPath));
    }

    public WorldMap Generate(WorldGenerationRequest request)
    {
        var tileMap = _loader.Load(_mapPath);
        var rooms = new List<WorldRoom>();
        var seed = request.Seed ?? 9001;
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        for (var y = 0; y < tileMap.Height; y++)
        {
            for (var x = 0; x < tileMap.Width; x++)
            {
                var tile = new WorldPosition(x, y);

                if (tileMap.IsBlocked(tile))
                {
                    continue;
                }

                var position = ToWorldPosition(tileMap, tile);
                var roomType = GetRoomType(tileMap, tile);
                var encounterSeed = roomType == RoomType.Enemy
                    ? DeriveEncounterSeed(seed, position)
                    : (int?)null;

                rooms.Add(new WorldRoom(position, roomType, encounterSeed));
                minX = Math.Min(minX, position.X);
                minY = Math.Min(minY, position.Y);
                maxX = Math.Max(maxX, position.X);
                maxY = Math.Max(maxY, position.Y);
            }
        }

        if (!rooms.Any(room => room.Position == WorldPosition.Origin && room.RoomType == RoomType.Start))
        {
            rooms.RemoveAll(room => room.Position == WorldPosition.Origin);
            rooms.Add(new WorldRoom(WorldPosition.Origin, RoomType.Start));
        }

        if (rooms.Count(room => room.RoomType == RoomType.Exit) == 0)
        {
            var exitRoom = rooms
                .Where(room => room.Position != WorldPosition.Origin)
                .OrderByDescending(room => room.Position.ManhattanDistanceFromOrigin)
                .First();
            rooms.Remove(exitRoom);
            rooms.Add(new WorldRoom(exitRoom.Position, RoomType.Exit));
        }

        return new WorldMap(seed, Math.Min(minX, minY), Math.Max(maxX, maxY), rooms);
    }

    private static WorldPosition ToWorldPosition(TileMapDefinition map, WorldPosition tile)
    {
        return new WorldPosition(tile.X - map.SpawnTile.X, map.SpawnTile.Y - tile.Y);
    }

    private static RoomType GetRoomType(TileMapDefinition map, WorldPosition tile)
    {
        if (map.SpawnTile == tile || map.FindObjectAt(tile, "spawn", "start") is not null)
        {
            return RoomType.Start;
        }

        if (map.FindObjectAt(tile, "exit") is not null)
        {
            return RoomType.Exit;
        }

        if (map.FindObjectAt(tile, "enemy", "encounter") is not null)
        {
            return RoomType.Enemy;
        }

        if (map.FindObjectAt(tile, "treasure", "chest") is not null)
        {
            return RoomType.Treasure;
        }

        return RoomType.Empty;
    }

    private static int DeriveEncounterSeed(int mapSeed, WorldPosition position)
    {
        unchecked
        {
            var hash = 23;
            hash = hash * 31 + mapSeed;
            hash = hash * 31 + position.X;
            hash = hash * 31 + position.Y;
            hash = hash * 31 + 2609;
            return hash;
        }
    }
}
