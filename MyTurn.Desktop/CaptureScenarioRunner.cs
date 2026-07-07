using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Presentation;
using AppWorldLayout = MyTurn.Application.WorldLayout;

namespace MyTurn.Desktop;

internal sealed class CaptureScenarioRunner
{
    private readonly DesktopRenderCaptureService _captureService;
    private readonly TemplateWorldGenerator _worldGenerator;

    public CaptureScenarioRunner(
        DesktopRenderCaptureService captureService,
        TemplateWorldGenerator worldGenerator)
    {
        _captureService = captureService;
        _worldGenerator = worldGenerator;
    }

    public void CaptureAll(string directory)
    {
        Directory.CreateDirectory(directory);

        foreach (var scenario in BuildScenarios())
        {
            _captureService.Capture(scenario.View, Path.Combine(directory, $"{scenario.Name}.png"));
        }
    }

    private IReadOnlyList<CaptureScenario> BuildScenarios()
    {
        var mainMenu = new MainMenuViewModel(
        [
            new("Quick Start", null, true),
            new("New Game"),
            new("Load Game"),
            new("Exit")
        ]);
        var party = CreatePartySummary();
        var hub = new HubViewModel(
            party,
            [
                new("Explore World", null, true),
                new("View Party"),
                new("Inventory"),
                new("Exit")
            ]);
        var startLayout = FindLayout(layout => layout.Objects.Count > 0);
        var enemyLayout = FindLayout(layout => layout.Objects.Any(obj => obj.ObjectType == WorldObjectType.Enemy));
        var objectLayout = FindLayout(layout => layout.Objects.Any(obj => obj.ObjectType is WorldObjectType.Treasure or WorldObjectType.Key or WorldObjectType.LockedDoor));
        var startWorld = CreateWorldView(startLayout, WorldPosition.Origin, party, null);
        var enemy = enemyLayout.Objects.First(obj => obj.ObjectType == WorldObjectType.Enemy);
        var enemyWorld = CreateWorldView(enemyLayout, enemy.Position, party, enemy);
        var promptObject = objectLayout.Objects.First(obj => obj.ObjectType is WorldObjectType.Treasure or WorldObjectType.Key or WorldObjectType.LockedDoor);
        var objectWorld = CreateWorldView(objectLayout, promptObject.Position, party, promptObject);
        var combat = new CombatViewModel(
            321,
            [
                new(Guid.NewGuid(), "Shade", CombatTeam.Enemy, 18, 30, true, false, false),
                new(Guid.NewGuid(), "Crawler", CombatTeam.Enemy, 12, 24, true, false, false)
            ],
            [
                new(Guid.NewGuid(), "Ari", CombatTeam.Player, 32, 44, true, false, true),
                new(Guid.NewGuid(), "Bram", CombatTeam.Player, 27, 36, true, false, false)
            ],
            "Ari",
            CombatTeam.Player,
            [new("Attack", null, true), new("Use Consumable"), new("Defend"), new("Change Equipment")],
            [],
            [],
            [],
            ["Encounter! 2 enemies appear.", "Ari is ready."],
            null,
            null);

        return
        [
            new("main-menu", mainMenu),
            new("hub", hub),
            new("explore-start", startWorld),
            new("explore-enemy-prompt", enemyWorld),
            new("explore-object-prompt", objectWorld),
            new("combat", combat)
        ];
    }

    private AppWorldLayout FindLayout(Func<AppWorldLayout, bool> predicate)
    {
        for (var seed = 1000; seed < 1100; seed++)
        {
            var layout = _worldGenerator.GenerateLayout(new WorldGenerationRequest(seed));

            if (predicate(layout))
            {
                return layout;
            }
        }

        return _worldGenerator.GenerateLayout(new WorldGenerationRequest(1000));
    }

    private WorldViewModel CreateWorldView(
        AppWorldLayout layout,
        WorldPosition currentPosition,
        PartySummaryViewModel party,
        WorldObject? promptObject)
    {
        var session = new WorldSession(
            layout.Map,
            currentPosition,
            layoutId: layout.LayoutId,
            profileId: layout.ProfileId,
            layoutSource: layout.LayoutSource,
            objects: layout.Objects);
        var tileMap = _worldGenerator.GetTileMap(session)
            ?? throw new InvalidOperationException("Capture world layout did not produce a tile map.");
        var tileMapView = CreateTileMapView(tileMap);
        var playerTile = ToTilePosition(session.CurrentPosition, tileMap);
        var camera = CreateCamera(playerTile, tileMap);
        var objects = session.Objects
            .Select(obj => new WorldObjectViewModel(
                obj.Id,
                obj.ObjectType.GetDisplayName(),
                ToTilePosition(obj.Position, tileMap),
                obj.State.GetDisplayName(),
                obj.IsBlocking,
                obj.IsActive))
            .ToArray();
        var prompt = CreatePrompt(promptObject);

        return new WorldViewModel(
            layout.Map.Seed,
            layout.Map.MinCoordinate,
            layout.Map.MaxCoordinate,
            session.CurrentPosition,
            [],
            party,
            session.CurrentRoom.RoomType.GetDisplayName(),
            "Active",
            prompt is null ? null : $"{prompt.Title}: {prompt.Message}",
            promptObject?.ObjectType == WorldObjectType.Enemy,
            false,
            tileMapView,
            playerTile.X * tileMap.TileWidth,
            playerTile.Y * tileMap.TileHeight,
            Direction.South,
            false,
            1f,
            promptObject?.ObjectType.GetDisplayName(),
            camera,
            objects,
            prompt);
    }

    private static WorldInteractionPromptViewModel? CreatePrompt(WorldObject? promptObject)
    {
        if (promptObject is null)
        {
            return null;
        }

        var (title, message) = promptObject.ObjectType switch
        {
            WorldObjectType.Enemy => ("Enemy", "Start battle?"),
            WorldObjectType.Treasure => ("Treasure", "Open the chest?"),
            WorldObjectType.Key => ("Key", "Pick up the key?"),
            WorldObjectType.Pickup => ("Pickup", "Pick this up?"),
            WorldObjectType.Door => ("Door", "Open the door?"),
            WorldObjectType.LockedDoor => ("Locked Door", "Unlock the door?"),
            WorldObjectType.Exit => ("Exit", "Leave this world?"),
            WorldObjectType.Npc => ("Talk", "Speak with them?"),
            WorldObjectType.Sign => ("Read", "Read the sign?"),
            _ => (promptObject.ObjectType.GetDisplayName(), "Interact?")
        };

        return new WorldInteractionPromptViewModel(
            promptObject.Id,
            promptObject.ObjectType.GetDisplayName(),
            title,
            message,
            true);
    }

    private static TileMapViewModel CreateTileMapView(TileMapDefinition tileMap)
    {
        var layers = tileMap.Layers
            .Select(layer => new TileLayerViewModel(
                layer.Name,
                layer.Width,
                layer.Height,
                layer.Visible,
                Enumerable.Range(0, layer.Height)
                    .SelectMany(y => Enumerable.Range(0, layer.Width)
                        .Select(x =>
                        {
                            var position = new WorldPosition(x, y);
                            var globalId = layer.GetTile(x, y);
                            var tile = tileMap.FindTile(globalId);

                            return new TileViewModel(position, globalId, tile?.Type ?? string.Empty, tile?.IsBlocking ?? false);
                        }))
                    .ToArray()))
            .ToArray();
        var firstTileset = tileMap.Tilesets.FirstOrDefault();

        return new TileMapViewModel(
            tileMap.Name,
            tileMap.Width,
            tileMap.Height,
            tileMap.TileWidth,
            tileMap.TileHeight,
            layers,
            tileMap.Objects.Select(obj => new TileObjectViewModel(obj.Name, obj.Type, obj.TilePosition)).ToArray(),
            firstTileset?.Columns ?? 0,
            firstTileset?.TileCount ?? 0);
    }

    private static WorldCameraViewModel CreateCamera(WorldPosition playerTile, TileMapDefinition tileMap)
    {
        const int columns = 17;
        const int rows = 11;
        var maxMinX = Math.Max(0, tileMap.Width - columns);
        var maxMinY = Math.Max(0, tileMap.Height - rows);
        var minX = Math.Clamp(playerTile.X - (columns / 2), 0, maxMinX);
        var minY = Math.Clamp(playerTile.Y - (rows / 2), 0, maxMinY);
        var visibleColumns = Math.Min(columns, tileMap.Width);
        var visibleRows = Math.Min(rows, tileMap.Height);

        return new WorldCameraViewModel(
            minX,
            minY,
            minX + visibleColumns - 1,
            minY + visibleRows - 1,
            visibleColumns,
            visibleRows,
            -minX * tileMap.TileWidth,
            -minY * tileMap.TileHeight);
    }

    private static WorldPosition ToTilePosition(WorldPosition worldPosition, TileMapDefinition tileMap)
    {
        return new WorldPosition(
            tileMap.SpawnTile.X + worldPosition.X,
            tileMap.SpawnTile.Y - worldPosition.Y);
    }

    private static PartySummaryViewModel CreatePartySummary()
    {
        return new PartySummaryViewModel(
            "Ari",
            2,
            4,
            18,
            42,
            [
                new(Guid.NewGuid(), "Ari", "Warrior", 32, 44, "Rusty Sword", "Active"),
                new(Guid.NewGuid(), "Bram", "Rogue", 27, 36, "Training Bow", "Active")
            ]);
    }

    private sealed record CaptureScenario(string Name, GameViewModel View);
}
