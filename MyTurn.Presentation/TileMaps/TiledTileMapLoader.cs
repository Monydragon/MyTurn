using System.Text.Json;
using System.Text.Json.Serialization;
using MyTurn.Domain;

namespace MyTurn.Presentation;

public sealed class TiledTileMapLoader : ITileMapLoader
{
    public TileMapDefinition Load(string mapPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapPath);

        var fullPath = Path.GetFullPath(mapPath);
        var directory = Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory;
        var document = JsonSerializer.Deserialize<TiledMapDocument>(File.ReadAllText(fullPath), JsonOptions)
            ?? throw new InvalidOperationException($"Map '{mapPath}' could not be parsed.");

        if (!string.Equals(document.Orientation, "orthogonal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only orthogonal Tiled maps are supported.");
        }

        var tilesets = document.Tilesets
            .Select(tileset => LoadTileset(directory, tileset))
            .ToArray();
        var layers = document.Layers
            .Where(layer => string.Equals(layer.Type, "tilelayer", StringComparison.OrdinalIgnoreCase))
            .Select(layer => new TileLayerDefinition(
                layer.Name,
                layer.Width ?? document.Width,
                layer.Height ?? document.Height,
                layer.Visible,
                layer.Data ?? []))
            .ToArray();
        var objects = document.Layers
            .Where(layer => string.Equals(layer.Type, "objectgroup", StringComparison.OrdinalIgnoreCase))
            .SelectMany(layer => layer.Objects ?? [])
            .Select(obj => CreateObject(obj, document.TileWidth, document.TileHeight))
            .ToArray();
        var spawn = objects.FirstOrDefault(obj => IsType(obj, "spawn"))?.TilePosition ?? WorldPosition.Origin;

        return new TileMapDefinition(
            Path.GetFileNameWithoutExtension(fullPath),
            document.Width,
            document.Height,
            document.TileWidth,
            document.TileHeight,
            layers,
            tilesets,
            objects,
            spawn);
    }

    private static TilesetDefinition LoadTileset(string mapDirectory, TiledTilesetReference reference)
    {
        if (string.IsNullOrWhiteSpace(reference.Source))
        {
            return new TilesetDefinition(
                reference.FirstGlobalId,
                reference.Name ?? "Embedded",
                null,
                reference.Image,
                reference.TileWidth,
                reference.TileHeight,
                reference.TileCount,
                reference.Columns,
                CreateTileDefinitions(reference.Tiles));
        }

        var tilesetPath = Path.GetFullPath(Path.Combine(mapDirectory, reference.Source));
        var tileset = JsonSerializer.Deserialize<TiledTilesetReference>(File.ReadAllText(tilesetPath), JsonOptions)
            ?? throw new InvalidOperationException($"Tileset '{reference.Source}' could not be parsed.");

        return new TilesetDefinition(
            reference.FirstGlobalId,
            tileset.Name ?? Path.GetFileNameWithoutExtension(tilesetPath),
            reference.Source,
            tileset.Image,
            tileset.TileWidth,
            tileset.TileHeight,
            tileset.TileCount,
            tileset.Columns,
            CreateTileDefinitions(tileset.Tiles));
    }

    private static IReadOnlyDictionary<int, TileDefinition> CreateTileDefinitions(IReadOnlyList<TiledTileDocument>? tiles)
    {
        var definitions = new Dictionary<int, TileDefinition>();

        foreach (var tile in tiles ?? [])
        {
            var properties = ReadProperties(tile.Properties);
            var type = tile.Type ?? ReadProperty(properties, "type") ?? string.Empty;
            var isBlocking = ReadBool(properties, "blocked") || ReadBool(properties, "collision");
            var eventType = ReadProperty(properties, "event");

            definitions[tile.Id] = new TileDefinition(tile.Id, type, isBlocking, eventType, properties);
        }

        return definitions;
    }

    private static TileObjectDefinition CreateObject(TiledObjectDocument obj, int tileWidth, int tileHeight)
    {
        var properties = ReadProperties(obj.Properties);
        var tileX = (int)Math.Floor(obj.X / Math.Max(1, tileWidth));
        var tileY = (int)Math.Floor(obj.Y / Math.Max(1, tileHeight));
        var type = string.IsNullOrWhiteSpace(obj.Type)
            ? ReadProperty(properties, "type") ?? obj.Name
            : obj.Type;
        var isBlocking = ReadBool(properties, "blocked") ||
            ReadBool(properties, "collision") ||
            string.Equals(type, "collision", StringComparison.OrdinalIgnoreCase);

        return new TileObjectDefinition(
            obj.Name,
            type,
            new WorldPosition(tileX, tileY),
            (int)Math.Ceiling(obj.Width / Math.Max(1, tileWidth)),
            (int)Math.Ceiling(obj.Height / Math.Max(1, tileHeight)),
            isBlocking,
            properties);
    }

    private static IReadOnlyDictionary<string, string> ReadProperties(IReadOnlyList<TiledPropertyDocument>? properties)
    {
        return properties?
            .Where(property => !string.IsNullOrWhiteSpace(property.Name))
            .ToDictionary(
                property => property.Name,
                property => property.Value?.ToString() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> properties, string key)
    {
        return properties.TryGetValue(key, out var value) &&
            bool.TryParse(value, out var result) &&
            result;
    }

    private static string? ReadProperty(IReadOnlyDictionary<string, string> properties, string key)
    {
        return properties.TryGetValue(key, out var value) ? value : null;
    }

    private static bool IsType(TileObjectDefinition obj, string type)
    {
        return string.Equals(obj.Type, type, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record TiledMapDocument(
        int Width,
        int Height,
        int TileWidth,
        int TileHeight,
        string Orientation,
        IReadOnlyList<TiledLayerDocument> Layers,
        IReadOnlyList<TiledTilesetReference> Tilesets);

    private sealed record TiledLayerDocument(
        string Name,
        string Type,
        bool Visible,
        int? Width,
        int? Height,
        IReadOnlyList<int>? Data,
        IReadOnlyList<TiledObjectDocument>? Objects);

    private sealed record TiledTilesetReference(
        [property: JsonPropertyName("firstgid")]
        int FirstGlobalId,
        string? Source,
        string? Name,
        string? Image,
        int TileWidth,
        int TileHeight,
        int TileCount,
        int Columns,
        IReadOnlyList<TiledTileDocument>? Tiles);

    private sealed record TiledTileDocument(
        int Id,
        string? Type,
        IReadOnlyList<TiledPropertyDocument>? Properties);

    private sealed record TiledObjectDocument(
        int Id,
        string Name,
        string Type,
        double X,
        double Y,
        double Width,
        double Height,
        IReadOnlyList<TiledPropertyDocument>? Properties);

    private sealed record TiledPropertyDocument(
        string Name,
        string Type,
        JsonElement? Value);
}
