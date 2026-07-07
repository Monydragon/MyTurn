using System.Text.Json;
using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Presentation;

public sealed class TemplateWorldGenerator : IWorldLayoutGenerator, IWorldTileMapProvider
{
    private const int FloorGid = 1;
    private const int WallGid = 2;

    private readonly MapGenerationProfile _profile;
    private readonly RoomTemplateCatalog _templates;
    private readonly Dictionary<string, TileMapDefinition> _tileMaps = [];

    public TemplateWorldGenerator(MapGenerationProfile profile, RoomTemplateCatalog templates)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
    }

    public WorldMap Generate(WorldGenerationRequest request)
    {
        return GenerateLayout(request).Map;
    }

    public WorldLayout GenerateLayout(WorldGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var seed = request.Seed ?? Environment.TickCount;
        var random = new SeededRandomSource(seed);
        var layoutId = $"{_profile.Id}:{seed}";
        var cells = BuildRoomCells(random);
        var roomKinds = ChooseRoomKinds(random, cells.Count);
        var width = _profile.GridColumns * _profile.RoomTileWidth;
        var height = _profile.GridRows * _profile.RoomTileHeight;
        var layerTiles = Enumerable.Repeat(WallGid, width * height).ToArray();
        var mapObjects = new List<TileObjectDefinition>();

        for (var index = 0; index < cells.Count; index++)
        {
            var cell = cells[index];
            var kind = roomKinds[index];
            var template = ChooseTemplate(kind, random);
            var roomOffset = new WorldPosition(
                cell.X * _profile.RoomTileWidth,
                cell.Y * _profile.RoomTileHeight);

            CopyTemplate(template.Map, layerTiles, width, roomOffset);
            CopyTemplateObjects(template, kind, mapObjects, roomOffset);
        }

        CarveDoorways(cells, layerTiles, width);

        var spawn = mapObjects.FirstOrDefault(obj => IsObjectType(obj, "spawn"))?.TilePosition
            ?? RoomCenter(cells[0]);
        EnsureRequiredObject(mapObjects, "spawn", "Spawn", spawn);
        EnsureRequiredObject(mapObjects, "exit", "Exit", RoomCenter(cells[^1]));

        var tileMap = new TileMapDefinition(
            "generated_dungeon",
            width,
            height,
            16,
            16,
            [new TileLayerDefinition("Ground", width, height, true, layerTiles)],
            [CreateGeneratedTileset()],
            mapObjects,
            spawn);
        var worldMap = CreateWorldMap(seed, tileMap);
        var worldObjects = CreateWorldObjects(layoutId, seed, tileMap).ToArray();

        _tileMaps[layoutId] = tileMap;

        return new WorldLayout(
            worldMap,
            layoutId,
            _profile.Id,
            "template-generator",
            worldObjects);
    }

    public TileMapDefinition? GetTileMap(WorldSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!string.IsNullOrWhiteSpace(session.LayoutId) &&
            _tileMaps.TryGetValue(session.LayoutId, out var cached))
        {
            return cached;
        }

        if (string.Equals(session.ProfileId, _profile.Id, StringComparison.OrdinalIgnoreCase))
        {
            var layout = GenerateLayout(new WorldGenerationRequest(session.Map.Seed));
            return _tileMaps.TryGetValue(layout.LayoutId, out var generated) ? generated : null;
        }

        return null;
    }

    private IReadOnlyList<GridCell> BuildRoomCells(IRandomSource random)
    {
        var target = Math.Clamp(_profile.PlacedRooms, 2, _profile.GridColumns * _profile.GridRows);
        var start = new GridCell(0, _profile.GridRows - 1);
        var cells = new List<GridCell> { start };
        var visited = new HashSet<GridCell> { start };
        var current = start;

        while (cells.Count < target)
        {
            var candidates = GetNeighbors(current)
                .Where(IsInBounds)
                .Where(cell => !visited.Contains(cell))
                .ToArray();

            if (candidates.Length == 0)
            {
                var expandable = cells
                    .Where(cell => GetNeighbors(cell).Any(candidate => IsInBounds(candidate) && !visited.Contains(candidate)))
                    .ToArray();

                if (expandable.Length == 0)
                {
                    break;
                }

                current = expandable[random.NextInclusive(0, expandable.Length - 1)];
                continue;
            }

            current = candidates[random.NextInclusive(0, candidates.Length - 1)];
            visited.Add(current);
            cells.Add(current);
        }

        return cells;
    }

    private IReadOnlyList<string> ChooseRoomKinds(IRandomSource random, int count)
    {
        var kinds = new string[count];
        kinds[0] = "start";
        kinds[^1] = "exit";
        var hasKey = false;
        var hasLockedDoor = false;

        for (var index = 1; index < count - 1; index++)
        {
            var roll = random.NextInclusive(1, 10000) / 10000d;
            var enemy = _profile.EnemyWeight;
            var treasure = enemy + _profile.TreasureWeight;
            var hazard = treasure + _profile.HazardWeight;
            var locked = hazard + _profile.LockedDoorWeight;

            if (roll <= enemy)
            {
                kinds[index] = "enemy";
            }
            else if (roll <= treasure)
            {
                kinds[index] = "treasure";
            }
            else if (roll <= hazard)
            {
                kinds[index] = "hazard";
            }
            else if (roll <= locked)
            {
                kinds[index] = "locked-door";
                hasLockedDoor = true;
            }
            else if (random.NextInclusive(1, 10000) / 10000d <= _profile.BranchChance)
            {
                kinds[index] = "npc";
            }
            else
            {
                kinds[index] = "corridor";
            }
        }

        if (hasLockedDoor)
        {
            for (var index = 1; index < count - 1; index++)
            {
                if (kinds[index] is "treasure" or "corridor" or "npc")
                {
                    kinds[index] = "key";
                    hasKey = true;
                    break;
                }
            }
        }

        if (hasLockedDoor && !hasKey && count > 2)
        {
            kinds[1] = "key";
        }

        return kinds;
    }

    private RoomTemplateDefinition ChooseTemplate(string kind, IRandomSource random)
    {
        var candidates = _templates.FindByTag(kind);

        if (candidates.Count == 0)
        {
            candidates = _templates.FindByTag("corridor");
        }

        if (candidates.Count == 0)
        {
            return _templates.Templates[0];
        }

        return candidates[random.NextInclusive(0, candidates.Count - 1)];
    }

    private void CopyTemplate(TileMapDefinition template, int[] targetTiles, int targetWidth, WorldPosition offset)
    {
        var layer = template.Layers.FirstOrDefault(layer => layer.Visible);

        if (layer is null)
        {
            return;
        }

        for (var y = 0; y < Math.Min(_profile.RoomTileHeight, template.Height); y++)
        {
            for (var x = 0; x < Math.Min(_profile.RoomTileWidth, template.Width); x++)
            {
                var gid = layer.GetTile(x, y);

                if (gid == 0)
                {
                    continue;
                }

                targetTiles[((offset.Y + y) * targetWidth) + offset.X + x] = gid;
            }
        }
    }

    private void CopyTemplateObjects(
        RoomTemplateDefinition template,
        string kind,
        List<TileObjectDefinition> objects,
        WorldPosition offset)
    {
        foreach (var source in template.Map.Objects.Where(obj => !IsObjectType(obj, "connector")))
        {
            objects.Add(source with
            {
                Name = string.IsNullOrWhiteSpace(source.Name) ? kind : source.Name,
                TilePosition = new WorldPosition(source.TilePosition.X + offset.X, source.TilePosition.Y + offset.Y)
            });
        }
    }

    private void CarveDoorways(IReadOnlyList<GridCell> cells, int[] targetTiles, int targetWidth)
    {
        var set = cells.ToHashSet();
        var centerX = _profile.RoomTileWidth / 2;
        var centerY = _profile.RoomTileHeight / 2;

        foreach (var cell in cells)
        {
            var roomX = cell.X * _profile.RoomTileWidth;
            var roomY = cell.Y * _profile.RoomTileHeight;

            if (set.Contains(cell with { X = cell.X + 1 }))
            {
                targetTiles[((roomY + centerY) * targetWidth) + roomX + _profile.RoomTileWidth - 1] = FloorGid;
                targetTiles[((roomY + centerY) * targetWidth) + roomX + _profile.RoomTileWidth] = FloorGid;
            }

            if (set.Contains(cell with { Y = cell.Y + 1 }))
            {
                targetTiles[((roomY + _profile.RoomTileHeight - 1) * targetWidth) + roomX + centerX] = FloorGid;
                targetTiles[((roomY + _profile.RoomTileHeight) * targetWidth) + roomX + centerX] = FloorGid;
            }
        }
    }

    private WorldMap CreateWorldMap(int seed, TileMapDefinition tileMap)
    {
        var rooms = new List<WorldRoom>();
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
                var encounterSeed = roomType == RoomType.Enemy ? DeriveEncounterSeed(seed, position) : (int?)null;

                rooms.Add(new WorldRoom(position, roomType, encounterSeed));
                minX = Math.Min(minX, position.X);
                minY = Math.Min(minY, position.Y);
                maxX = Math.Max(maxX, position.X);
                maxY = Math.Max(maxY, position.Y);
            }
        }

        EnsureDomainStartAndExit(seed, rooms, tileMap);

        return new WorldMap(seed, Math.Min(minX, minY), Math.Max(maxX, maxY), rooms);
    }

    private IEnumerable<WorldObject> CreateWorldObjects(string layoutId, int seed, TileMapDefinition tileMap)
    {
        var index = 0;

        foreach (var obj in tileMap.Objects)
        {
            if (!TryMapObjectType(obj.Type, out var objectType))
            {
                continue;
            }

            var position = ToWorldPosition(tileMap, obj.TilePosition);
            var id = $"{layoutId}:{objectType}:{position.X}:{position.Y}:{index++}";
            var blocking = objectType is WorldObjectType.Door or WorldObjectType.LockedDoor or WorldObjectType.BlockingProp;
            var encounterSeed = objectType == WorldObjectType.Enemy ? DeriveEncounterSeed(seed, position) : (int?)null;

            yield return new WorldObject(
                id,
                objectType,
                position,
                blocking,
                WorldObjectState.Active,
                encounterSeed,
                BuildPayloadJson(objectType, obj));
        }
    }

    private static TilesetDefinition CreateGeneratedTileset()
    {
        var tiles = new Dictionary<int, TileDefinition>
        {
            [0] = new(0, "floor", false, null, null),
            [1] = new(1, "wall", true, null, null),
            [2] = new(2, "path", false, null, null),
            [3] = new(3, "water", true, null, null)
        };

        return new TilesetDefinition(1, "prototype_tileset", "../Tiles/prototype_tileset.tsj", "prototype_tileset.png", 16, 16, 16, 4, tiles);
    }

    private static void EnsureRequiredObject(
        List<TileObjectDefinition> objects,
        string type,
        string name,
        WorldPosition position)
    {
        if (objects.Any(obj => IsObjectType(obj, type)))
        {
            return;
        }

        objects.Add(new TileObjectDefinition(name, type, position, 1, 1, false, new Dictionary<string, string>()));
    }

    private void EnsureDomainStartAndExit(int seed, List<WorldRoom> rooms, TileMapDefinition tileMap)
    {
        var originRoom = rooms.SingleOrDefault(room => room.Position == WorldPosition.Origin);

        if (originRoom is null || originRoom.RoomType != RoomType.Start)
        {
            rooms.RemoveAll(room => room.Position == WorldPosition.Origin);
            rooms.Add(new WorldRoom(WorldPosition.Origin, RoomType.Start));
        }

        if (rooms.Count(room => room.RoomType == RoomType.Exit) == 0)
        {
            var exitObject = tileMap.Objects.First(obj => IsObjectType(obj, "exit"));
            var position = ToWorldPosition(tileMap, exitObject.TilePosition);
            rooms.RemoveAll(room => room.Position == position);
            rooms.Add(new WorldRoom(position, RoomType.Exit, DeriveEncounterSeed(seed, position)));
        }
    }

    private WorldPosition RoomCenter(GridCell cell)
    {
        return new WorldPosition(
            (cell.X * _profile.RoomTileWidth) + (_profile.RoomTileWidth / 2),
            (cell.Y * _profile.RoomTileHeight) + (_profile.RoomTileHeight / 2));
    }

    private bool IsInBounds(GridCell cell)
    {
        return cell.X >= 0 && cell.Y >= 0 && cell.X < _profile.GridColumns && cell.Y < _profile.GridRows;
    }

    private static IReadOnlyList<GridCell> GetNeighbors(GridCell cell)
    {
        return
        [
            cell with { X = cell.X + 1 },
            cell with { X = cell.X - 1 },
            cell with { Y = cell.Y + 1 },
            cell with { Y = cell.Y - 1 }
        ];
    }

    private static RoomType GetRoomType(TileMapDefinition map, WorldPosition tile)
    {
        if (map.SpawnTile == tile || map.FindObjectAt(tile, "spawn", "start") is not null)
        {
            return RoomType.Start;
        }

        if (map.FindObjectAt(tile, "exit", "stairs") is not null)
        {
            return RoomType.Exit;
        }

        if (map.FindObjectAt(tile, "enemy", "encounter") is not null)
        {
            return RoomType.Enemy;
        }

        return RoomType.Empty;
    }

    private static WorldPosition ToWorldPosition(TileMapDefinition map, WorldPosition tile)
    {
        return new WorldPosition(tile.X - map.SpawnTile.X, map.SpawnTile.Y - tile.Y);
    }

    private static bool IsObjectType(TileObjectDefinition obj, string type)
    {
        return string.Equals(obj.Type, type, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryMapObjectType(string type, out WorldObjectType objectType)
    {
        switch (type.ToLowerInvariant())
        {
            case "enemy":
            case "encounter":
                objectType = WorldObjectType.Enemy;
                return true;
            case "treasure":
            case "chest":
                objectType = WorldObjectType.Treasure;
                return true;
            case "exit":
            case "stairs":
                objectType = WorldObjectType.Exit;
                return true;
            case "door":
                objectType = WorldObjectType.Door;
                return true;
            case "locked-door":
            case "lockeddoor":
                objectType = WorldObjectType.LockedDoor;
                return true;
            case "key":
                objectType = WorldObjectType.Key;
                return true;
            case "pickup":
                objectType = WorldObjectType.Pickup;
                return true;
            case "hazard":
                objectType = WorldObjectType.Hazard;
                return true;
            case "npc":
                objectType = WorldObjectType.Npc;
                return true;
            case "sign":
                objectType = WorldObjectType.Sign;
                return true;
            case "blocking-prop":
            case "blockingprop":
                objectType = WorldObjectType.BlockingProp;
                return true;
            default:
                objectType = default;
                return false;
        }
    }

    private static string BuildPayloadJson(WorldObjectType objectType, TileObjectDefinition obj)
    {
        var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in obj.Properties)
        {
            payload[property.Key] = property.Value;
        }

        switch (objectType)
        {
            case WorldObjectType.Treasure:
                payload.TryAdd("currency", "18");
                payload.TryAdd("itemId", "small-healing-potion");
                payload.TryAdd("quantity", "1");
                payload.TryAdd("message", "You open the chest.");
                break;
            case WorldObjectType.Key:
                payload.TryAdd("keyId", "bronze-key");
                payload.TryAdd("message", "You found a bronze key.");
                break;
            case WorldObjectType.Pickup:
                payload.TryAdd("itemId", "small-healing-potion");
                payload.TryAdd("quantity", "1");
                payload.TryAdd("message", "You picked up a potion.");
                break;
            case WorldObjectType.Hazard:
                payload.TryAdd("damage", "3");
                payload.TryAdd("message", "A hidden trap snaps shut.");
                break;
            case WorldObjectType.LockedDoor:
                payload.TryAdd("keyId", "bronze-key");
                payload.TryAdd("message", "A locked door blocks the way.");
                break;
            case WorldObjectType.Npc:
            case WorldObjectType.Sign:
                payload.TryAdd("message", "The path ahead twists deeper into the ruins.");
                break;
        }

        return JsonSerializer.Serialize(payload);
    }

    private static int DeriveEncounterSeed(int mapSeed, WorldPosition position)
    {
        unchecked
        {
            var hash = 23;
            hash = hash * 31 + mapSeed;
            hash = hash * 31 + position.X;
            hash = hash * 31 + position.Y;
            hash = hash * 31 + 9011;
            return hash;
        }
    }

    private readonly record struct GridCell(int X, int Y);
}
