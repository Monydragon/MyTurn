using MyTurn.Domain;

namespace MyTurn.Presentation;

public sealed record RoomTemplateDefinition(
    string Id,
    IReadOnlySet<string> Tags,
    TileMapDefinition Map)
{
    public bool HasTag(string tag)
    {
        return Tags.Contains(tag);
    }
}

public sealed record RoomTemplateCatalog(IReadOnlyList<RoomTemplateDefinition> Templates)
{
    public RoomTemplateDefinition GetByTag(string tag)
    {
        return Templates.FirstOrDefault(template => template.HasTag(tag))
            ?? throw new KeyNotFoundException($"No room template has tag '{tag}'.");
    }

    public IReadOnlyList<RoomTemplateDefinition> FindByTag(string tag)
    {
        return Templates.Where(template => template.HasTag(tag)).ToArray();
    }
}

public interface IRoomTemplateLoader
{
    RoomTemplateCatalog Load(MapGenerationProfile profile);
}

public sealed class TiledRoomTemplateLoader : IRoomTemplateLoader
{
    private readonly ITileMapLoader _tileMapLoader;

    public TiledRoomTemplateLoader(ITileMapLoader tileMapLoader)
    {
        _tileMapLoader = tileMapLoader ?? throw new ArgumentNullException(nameof(tileMapLoader));
    }

    public RoomTemplateCatalog Load(MapGenerationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new RoomTemplateCatalog(profile.Templates
            .Select(template => new RoomTemplateDefinition(
                template.Id,
                new HashSet<string>(template.Tags.Select(tag => tag.ToLowerInvariant())),
                _tileMapLoader.Load(template.Path)))
            .ToArray());
    }
}

public interface IWorldTileMapProvider
{
    TileMapDefinition? GetTileMap(WorldSession session);
}
