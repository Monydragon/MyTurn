using MyTurn.Domain;

namespace MyTurn.Presentation;

public sealed record TileMapDefinition(
    string Name,
    int Width,
    int Height,
    int TileWidth,
    int TileHeight,
    IReadOnlyList<TileLayerDefinition> Layers,
    IReadOnlyList<TilesetDefinition> Tilesets,
    IReadOnlyList<TileObjectDefinition> Objects,
    WorldPosition SpawnTile)
{
    public bool IsBlocked(WorldPosition tile)
    {
        if (tile.X < 0 || tile.Y < 0 || tile.X >= Width || tile.Y >= Height)
        {
            return true;
        }

        if (Objects.Any(obj => obj.TilePosition == tile && obj.IsBlocking))
        {
            return true;
        }

        foreach (var layer in Layers.Where(layer => layer.Visible))
        {
            var gid = layer.GetTile(tile.X, tile.Y);

            if (gid == 0)
            {
                continue;
            }

            var tileDefinition = FindTile(gid);

            if (tileDefinition?.IsBlocking == true)
            {
                return true;
            }
        }

        return false;
    }

    public TileDefinition? FindTile(int globalId)
    {
        var tileset = Tilesets
            .Where(candidate => candidate.FirstGlobalId <= globalId)
            .OrderByDescending(candidate => candidate.FirstGlobalId)
            .FirstOrDefault();

        return tileset?.GetTile(globalId - tileset.FirstGlobalId);
    }

    public TileObjectDefinition? FindObjectAt(WorldPosition tile, params string[] types)
    {
        return Objects.FirstOrDefault(obj =>
            obj.TilePosition == tile &&
            types.Any(type => string.Equals(type, obj.Type, StringComparison.OrdinalIgnoreCase)));
    }
}

public sealed record TileLayerDefinition(
    string Name,
    int Width,
    int Height,
    bool Visible,
    IReadOnlyList<int> Tiles)
{
    public int GetTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return 0;
        }

        return Tiles[y * Width + x];
    }
}

public sealed record TilesetDefinition(
    int FirstGlobalId,
    string Name,
    string? Source,
    string? Image,
    int TileWidth,
    int TileHeight,
    int TileCount,
    int Columns,
    IReadOnlyDictionary<int, TileDefinition> Tiles)
{
    public TileDefinition? GetTile(int localId)
    {
        return Tiles.TryGetValue(localId, out var tile)
            ? tile
            : new TileDefinition(localId, string.Empty, false, null, null);
    }
}

public sealed record TileDefinition(
    int LocalId,
    string Type,
    bool IsBlocking,
    string? EventType,
    IReadOnlyDictionary<string, string>? Properties);

public sealed record TileObjectDefinition(
    string Name,
    string Type,
    WorldPosition TilePosition,
    int Width,
    int Height,
    bool IsBlocking,
    IReadOnlyDictionary<string, string> Properties);

public sealed record TileAnimationViewModel(
    int TileId,
    int DurationMilliseconds);

public sealed record TileMapViewModel(
    string Name,
    int Width,
    int Height,
    int TileWidth,
    int TileHeight,
    IReadOnlyList<TileLayerViewModel> Layers,
    IReadOnlyList<TileObjectViewModel> Objects,
    int TilesetColumns,
    int TilesetTileCount);

public sealed record TileLayerViewModel(
    string Name,
    int Width,
    int Height,
    bool Visible,
    IReadOnlyList<TileViewModel> Tiles);

public sealed record TileViewModel(
    WorldPosition Position,
    int GlobalId,
    string Type,
    bool IsBlocking);

public sealed record TileObjectViewModel(
    string Name,
    string Type,
    WorldPosition Position);
