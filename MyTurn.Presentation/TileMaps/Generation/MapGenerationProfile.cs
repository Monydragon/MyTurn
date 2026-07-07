using System.Text.Json;

namespace MyTurn.Presentation;

public sealed record MapGenerationProfile(
    string Id,
    int GridColumns,
    int GridRows,
    int PlacedRooms,
    int RoomTileWidth,
    int RoomTileHeight,
    double EnemyWeight,
    double TreasureWeight,
    double HazardWeight,
    double LockedDoorWeight,
    double BranchChance,
    double LoopChance,
    IReadOnlyList<RoomTemplateProfileEntry> Templates)
{
    public static MapGenerationProfile Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var document = JsonSerializer.Deserialize<MapGenerationProfileDocument>(
            File.ReadAllText(fullPath),
            JsonOptions) ?? throw new InvalidOperationException($"Generation profile '{path}' could not be parsed.");
        var baseDirectory = Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory;

        return new MapGenerationProfile(
            document.Id,
            document.GridColumns,
            document.GridRows,
            document.PlacedRooms,
            document.RoomTileWidth,
            document.RoomTileHeight,
            document.EnemyWeight,
            document.TreasureWeight,
            document.HazardWeight,
            document.LockedDoorWeight,
            document.BranchChance,
            document.LoopChance,
            document.Templates
                .Select(template => new RoomTemplateProfileEntry(
                    template.Id,
                    Path.GetFullPath(Path.Combine(baseDirectory, template.Path)),
                    template.Tags))
                .ToArray());
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record MapGenerationProfileDocument(
        string Id,
        int GridColumns,
        int GridRows,
        int PlacedRooms,
        int RoomTileWidth,
        int RoomTileHeight,
        double EnemyWeight,
        double TreasureWeight,
        double HazardWeight,
        double LockedDoorWeight,
        double BranchChance,
        double LoopChance,
        IReadOnlyList<RoomTemplateProfileEntryDocument> Templates);

    private sealed record RoomTemplateProfileEntryDocument(
        string Id,
        string Path,
        IReadOnlyList<string> Tags);
}

public sealed record RoomTemplateProfileEntry(
    string Id,
    string Path,
    IReadOnlyList<string> Tags);
