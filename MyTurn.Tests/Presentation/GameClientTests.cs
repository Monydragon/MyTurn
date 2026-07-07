using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Presentation;
using MyTurn.Presentation.Input;

namespace MyTurn.Tests.Presentation;

[TestFixture]
public sealed class GameClientTests
{
    [Test]
    public void StartNewQuickGame_ReachesHub()
    {
        var client = new GameClient(CreateServices(CreateEmptyWorldGenerator()));

        client.StartNewQuickGame();

        var view = (HubViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(view.ScreenKind, Is.EqualTo(ScreenKind.Hub));
            Assert.That(view.Party.ActiveCount, Is.GreaterThan(0));
            Assert.That(view.Options, Is.Not.Empty);
        });
    }

    [Test]
    public void WorldMovement_UpdatesViewImmediately()
    {
        var client = new GameClient(CreateServices(CreateEmptyWorldGenerator()));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        client.Update(Press(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));

        var view = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(view.CurrentPosition, Is.EqualTo(new WorldPosition(1, 0)));
            Assert.That(view.Party.Steps, Is.EqualTo(1));
            Assert.That(view.IsMoving, Is.False);
            Assert.That(view.MovementProgress, Is.EqualTo(1f));
            Assert.That(view.HasPendingEncounter, Is.False);
        });
    }

    [Test]
    public void EnemyRoom_CreatesPendingEncounterImmediatelyBeforeCombat()
    {
        var client = new GameClient(CreateServices(CreateEnemyNorthWorldGenerator()));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        client.Update(Press(GameCommand.MoveUp), TimeSpan.FromMilliseconds(16));

        var pendingView = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(pendingView.ScreenKind, Is.EqualTo(ScreenKind.World));
            Assert.That(pendingView.IsMoving, Is.False);
            Assert.That(pendingView.HasPendingEncounter, Is.True);
            Assert.That(pendingView.CurrentPosition, Is.EqualTo(new WorldPosition(0, 1)));
        });

        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        Assert.That(client.CurrentView.ScreenKind, Is.EqualTo(ScreenKind.Combat));
    }

    [Test]
    public void PendingEncounter_CancelReturnsToHub()
    {
        var client = new GameClient(CreateServices(CreateEnemyNorthWorldGenerator()));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));
        client.Update(Press(GameCommand.MoveUp), TimeSpan.FromMilliseconds(16));

        client.Update(Press(GameCommand.Cancel), TimeSpan.FromMilliseconds(16));

        Assert.That(client.CurrentView.ScreenKind, Is.EqualTo(ScreenKind.Hub));
    }

    [Test]
    public void HeldMovement_MovesOnceAndWaitsForNeutralBeforeNextStep()
    {
        var client = new GameClient(CreateServices(CreateStraightWorldGenerator()));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        client.Update(Hold(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));
        client.Update(Hold(GameCommand.MoveRight), TimeSpan.FromMilliseconds(100));

        var view = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(view.CurrentPosition, Is.EqualTo(new WorldPosition(1, 0)));
            Assert.That(view.IsMoving, Is.False);
            Assert.That(view.Party.Steps, Is.EqualTo(1));
        });

        client.Update(InputSnapshot.Empty, TimeSpan.FromMilliseconds(16));
        client.Update(Hold(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));
        client.Update(Hold(GameCommand.MoveRight), TimeSpan.FromMilliseconds(100));

        view = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(view.CurrentPosition, Is.EqualTo(new WorldPosition(2, 0)));
            Assert.That(view.Party.Steps, Is.EqualTo(2));
        });
    }

    [Test]
    public void LegacyTreasureRoom_ResolvesImmediately()
    {
        var client = new GameClient(CreateServices(CreateTreasureEastWorldGenerator()));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        client.Update(Press(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));
        var view = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(view.LatestEvent, Is.Not.Null);
            Assert.That(view.RoomStatus, Is.EqualTo("Looted"));
        });
    }

    [Test]
    public void LegacyExitRoom_ResolvesImmediately()
    {
        var client = new GameClient(CreateServices(CreateExitEastWorldGenerator()));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        client.Update(Press(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));

        Assert.That(client.CurrentView.ScreenKind, Is.EqualTo(ScreenKind.Message));
    }

    [Test]
    public void ObjectTreasure_CreatesPromptAndConfirmClaimsReward()
    {
        var client = new GameClient(CreateServices(CreateObjectWorldGenerator(
        [
            new WorldObject("chest", WorldObjectType.Treasure, new WorldPosition(1, 0), payloadJson: "{\"currency\":\"10\",\"message\":\"A chest waits.\"}")
        ])));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        client.Update(Press(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));
        var promptView = (WorldViewModel)client.CurrentView;

        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));
        var claimedView = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(promptView.InteractionPrompt?.ObjectType, Is.EqualTo("Treasure"));
            Assert.That(claimedView.Party.Currency, Is.EqualTo(10));
            Assert.That(claimedView.Objects?.Single(obj => obj.Id == "chest").IsActive, Is.False);
        });
    }

    [Test]
    public void BlockingDoor_PreventsMovementUntilOpened()
    {
        var client = new GameClient(CreateServices(CreateObjectWorldGenerator(
        [
            new WorldObject("door", WorldObjectType.Door, new WorldPosition(1, 0), isBlocking: true)
        ])));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        client.Update(Press(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));
        var blockedView = (WorldViewModel)client.CurrentView;
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));
        client.Update(Press(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));
        var movedView = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(blockedView.CurrentPosition, Is.EqualTo(WorldPosition.Origin));
            Assert.That(blockedView.InteractionPrompt?.ObjectType, Is.EqualTo("Door"));
            Assert.That(movedView.CurrentPosition, Is.EqualTo(new WorldPosition(1, 0)));
        });
    }

    [Test]
    public void FacingSign_ConfirmCreatesAndResolvesPrompt()
    {
        var client = new GameClient(CreateServices(new FixedLayoutGenerator(CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(0, -1)] = RoomType.Empty,
            [new(1, 0)] = RoomType.Exit
        }),
        [
            new WorldObject("sign", WorldObjectType.Sign, new WorldPosition(0, -1), payloadJson: "{\"message\":\"The sign says hello.\"}")
        ])));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));
        var promptView = (WorldViewModel)client.CurrentView;
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));
        var messageView = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(promptView.InteractionPrompt?.ObjectType, Is.EqualTo("Sign"));
            Assert.That(messageView.LatestEvent, Does.Contain("hello"));
        });
    }

    [Test]
    public void ObjectExit_CompletesOnConfirm()
    {
        var client = new GameClient(CreateServices(new FixedLayoutGenerator(CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Exit
        }),
        [
            new WorldObject("stairs", WorldObjectType.Exit, new WorldPosition(1, 0))
        ])));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        client.Update(Press(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));
        var promptView = (WorldViewModel)client.CurrentView;
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        Assert.Multiple(() =>
        {
            Assert.That(promptView.InteractionPrompt?.ObjectType, Is.EqualTo("Exit"));
            Assert.That(client.CurrentView.ScreenKind, Is.EqualTo(ScreenKind.Message));
        });
    }

    [Test]
    public void GeneratedDesktopMap_MovementBuildsWorldViewWithoutHanging()
    {
        var generator = CreateTemplateWorldGenerator();
        var client = new GameClient(CreateServices(generator), tileMapProvider: generator);
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        var initial = (WorldViewModel)client.CurrentView;
        var moved = false;

        foreach (var command in new[] { GameCommand.MoveUp, GameCommand.MoveDown, GameCommand.MoveLeft, GameCommand.MoveRight })
        {
            client.Update(InputSnapshot.Empty, TimeSpan.FromMilliseconds(16));
            client.Update(Hold(command), TimeSpan.FromMilliseconds(16));
            var view = (WorldViewModel)client.CurrentView;

            if (view.CurrentPosition == initial.CurrentPosition)
            {
                continue;
            }

            var movedPosition = view.CurrentPosition;
            client.Update(Hold(command), TimeSpan.FromMilliseconds(16));
            var heldAgainView = (WorldViewModel)client.CurrentView;

            moved = true;
            Assert.Multiple(() =>
            {
                Assert.That(view.IsMoving, Is.False);
                Assert.That(view.MovementProgress, Is.EqualTo(1f));
                Assert.That(view.Camera, Is.Not.Null);
                Assert.That(view.TileMap, Is.Not.Null);
                Assert.That(heldAgainView.CurrentPosition, Is.EqualTo(movedPosition));
            });
            break;
        }

        Assert.That(moved, Is.True, "At least one direction from the generated spawn should be walkable.");
    }

    [Test]
    public void BattleVictory_ReturnsToWorldWithoutSynchronousPersistenceSave()
    {
        var persistence = new CountingPersistenceService();
        var client = new GameClient(CreateServices(CreateObjectWorldGenerator(
        [
            new WorldObject("enemy", WorldObjectType.Enemy, new WorldPosition(1, 0), encounterSeed: 2)
        ]), persistence));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));
        client.Update(Press(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        var saveCountBeforeBattleResolution = persistence.SaveCount;

        for (var i = 0; i < 400 && client.CurrentView.ScreenKind == ScreenKind.Combat; i++)
        {
            var input = client.CurrentView is CombatViewModel combat &&
                (combat.CommandOptions.Count > 0 || combat.TargetOptions.Count > 0 || combat.OutcomeTitle is not null)
                    ? Press(GameCommand.Confirm)
                    : InputSnapshot.Empty;

            client.Update(input, TimeSpan.FromMilliseconds(16));
        }

        var world = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(world.ScreenKind, Is.EqualTo(ScreenKind.World));
            Assert.That(world.HasPendingEncounter, Is.False);
            Assert.That(world.Objects?.Single(obj => obj.Id == "enemy").IsActive, Is.False);
            Assert.That(persistence.SaveCount, Is.EqualTo(saveCountBeforeBattleResolution));
        });
    }

    [Test]
    public void HazardTrigger_ResolvesWithoutSynchronousPersistenceSave()
    {
        var persistence = new CountingPersistenceService();
        var client = new GameClient(CreateServices(CreateObjectWorldGenerator(
        [
            new WorldObject("trap", WorldObjectType.Hazard, new WorldPosition(1, 0), payloadJson: "{\"message\":\"A trap snaps.\"}")
        ]), persistence));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        var saveCountBeforeTrigger = persistence.SaveCount;
        client.Update(Press(GameCommand.MoveRight), TimeSpan.FromMilliseconds(16));
        var world = (WorldViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(world.LatestEvent, Does.Contain("trap"));
            Assert.That(world.Objects?.Single(obj => obj.Id == "trap").IsActive, Is.False);
            Assert.That(persistence.SaveCount, Is.EqualTo(saveCountBeforeTrigger));
        });
    }

    [Test]
    public void Combat_ConfirmAttackShowsTargetMenu()
    {
        var client = new GameClient(CreateServices(CreateEmptyWorldGenerator()));
        client.StartNewQuickGame();
        client.Update(Press(GameCommand.MoveDown), TimeSpan.FromMilliseconds(16));
        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));

        AdvanceUntil(
            client,
            view => view is CombatViewModel combat && combat.CommandOptions.Count > 0);
        var actionView = (CombatViewModel)client.CurrentView;

        client.Update(Press(GameCommand.Confirm), TimeSpan.FromMilliseconds(16));
        var targetView = (CombatViewModel)client.CurrentView;

        Assert.Multiple(() =>
        {
            Assert.That(actionView.CommandOptions, Is.Not.Empty);
            Assert.That(targetView.TargetOptions, Is.Not.Empty);
        });
    }

    private static void AdvanceUntil(GameClient client, Func<GameViewModel, bool> predicate)
    {
        for (var i = 0; i < 12 && !predicate(client.CurrentView); i++)
        {
            client.Update(InputSnapshot.Empty, TimeSpan.FromMilliseconds(16));
        }
    }

    private static InputSnapshot Press(GameCommand command)
    {
        return new InputSnapshot([command], [command]);
    }

    private static InputSnapshot Hold(GameCommand command)
    {
        return new InputSnapshot([], [command]);
    }

    private static IWorldGenerator CreateEmptyWorldGenerator()
    {
        return new FixedWorldGenerator(CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Empty,
            [new(0, 1)] = RoomType.Empty,
            [new(1, 1)] = RoomType.Exit
        }));
    }

    private static IWorldGenerator CreateEnemyNorthWorldGenerator()
    {
        return new FixedWorldGenerator(new WorldMap(
            42,
            0,
            1,
            [
                new WorldRoom(new WorldPosition(0, 0), RoomType.Start),
                new WorldRoom(new WorldPosition(1, 0), RoomType.Empty),
                new WorldRoom(new WorldPosition(0, 1), RoomType.Enemy, encounterSeed: 123),
                new WorldRoom(new WorldPosition(1, 1), RoomType.Exit)
            ]));
    }

    private static IWorldGenerator CreateStraightWorldGenerator()
    {
        return new FixedWorldGenerator(CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Empty,
            [new(2, 0)] = RoomType.Empty,
            [new(3, 0)] = RoomType.Exit
        }));
    }

    private static IWorldGenerator CreateTreasureEastWorldGenerator()
    {
        return new FixedWorldGenerator(CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Treasure,
            [new(2, 0)] = RoomType.Exit
        }));
    }

    private static IWorldGenerator CreateExitEastWorldGenerator()
    {
        return new FixedWorldGenerator(CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Exit
        }));
    }

    private static IWorldGenerator CreateObjectWorldGenerator(IReadOnlyList<WorldObject> objects)
    {
        return new FixedLayoutGenerator(CreateMap(new Dictionary<WorldPosition, RoomType>
        {
            [new(0, 0)] = RoomType.Start,
            [new(1, 0)] = RoomType.Empty,
            [new(2, 0)] = RoomType.Exit
        }), objects);
    }

    private static TemplateWorldGenerator CreateTemplateWorldGenerator()
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

    private static WorldMap CreateMap(IReadOnlyDictionary<WorldPosition, RoomType> rooms)
    {
        var min = rooms.Keys.Min(position => Math.Min(position.X, position.Y));
        var max = rooms.Keys.Max(position => Math.Max(position.X, position.Y));

        return new WorldMap(42, min, max, rooms.Select(room => new WorldRoom(room.Key, room.Value)));
    }

    private static ApplicationServices CreateServices(
        IWorldGenerator worldGenerator,
        IGamePersistenceService? persistence = null)
    {
        var skillDefinitions = new DefaultSkillDefinitionRegistry();
        var statDefinitions = new DefaultStatDefinitionRegistry();
        var weaponDefinitions = new DefaultWeaponDefinitionRegistry();
        var itemDefinitions = new DefaultItemDefinitionRegistry(weaponDefinitions);
        var enemyDefinitions = new DefaultEnemyDefinitionRegistry();
        var characterCreationValidator = new CharacterCreationValidator();
        var inventoryService = new InventoryService(itemDefinitions);
        var startingEquipmentService = new StartingEquipmentService(weaponDefinitions);
        var equipmentService = new EquipmentService(weaponDefinitions);
        var lootService = new LootService(itemDefinitions);
        var encounterGenerator = new EncounterGenerator(enemyDefinitions);
        var combatService = new CombatService(equipmentService, itemDefinitions, lootService, statDefinitions);
        var treasureLootService = new TreasureLootService(itemDefinitions);
        var worldSessionService = new WorldSessionService(worldGenerator);
        var explorationService = new WorldExplorationService(encounterGenerator, treasureLootService);
        var minimapService = new MinimapService();
        var partyService = new PartyService();
        var recruitmentService = new RecruitmentService();
        var encounterDifficultyService = new EncounterDifficultyService();
        var gameFlowService = new GameFlowService();
        var actorFactory = new DefaultActorFactory(
            skillDefinitions,
            statDefinitions,
            startingEquipmentService,
            inventoryService,
            characterCreationValidator);
        var quickStartPartyFactory = new QuickStartPartyFactory(actorFactory, partyService);
        var skillExperienceService = new SkillExperienceService();

        return new ApplicationServices(
            actorFactory,
            characterCreationValidator,
            combatService,
            encounterGenerator,
            enemyDefinitions,
            equipmentService,
            explorationService,
            gameFlowService,
            inventoryService,
            itemDefinitions,
            lootService,
            minimapService,
            partyService,
            quickStartPartyFactory,
            recruitmentService,
            encounterDifficultyService,
            skillExperienceService,
            startingEquipmentService,
            treasureLootService,
            weaponDefinitions,
            worldGenerator,
            worldSessionService,
            skillDefinitions,
            statDefinitions,
            persistence);
    }

    private sealed class FixedWorldGenerator : IWorldGenerator
    {
        private readonly WorldMap _map;

        public FixedWorldGenerator(WorldMap map)
        {
            _map = map;
        }

        public WorldMap Generate(WorldGenerationRequest request)
        {
            return _map;
        }
    }

    private sealed class FixedLayoutGenerator : IWorldLayoutGenerator
    {
        private readonly WorldMap _map;
        private readonly IReadOnlyList<WorldObject> _objects;

        public FixedLayoutGenerator(WorldMap map, IReadOnlyList<WorldObject> objects)
        {
            _map = map;
            _objects = objects;
        }

        public WorldMap Generate(WorldGenerationRequest request)
        {
            return _map;
        }

        public WorldLayout GenerateLayout(WorldGenerationRequest request)
        {
            return new WorldLayout(_map, "fixed-layout", "test", "test", _objects);
        }
    }

    private sealed class CountingPersistenceService : IGamePersistenceService
    {
        private GameSession? _session;

        public int SaveCount { get; private set; }

        public IReadOnlyList<SaveSlotSummary> ListSaves()
        {
            return _session is null
                ? []
                : [new SaveSlotSummary(_session.SaveSlotId, "Test", DateTime.UtcNow, DateTime.UtcNow)];
        }

        public GameSession CreateSave(Party party)
        {
            _session = new GameSession(Guid.NewGuid(), party, null);
            return _session;
        }

        public GameSession CreateSave(Actor actor)
        {
            return CreateSave(new Party([actor], inventory: actor.Inventory));
        }

        public GameSession LoadSave(Guid saveSlotId)
        {
            return _session ?? throw new InvalidOperationException("No save exists.");
        }

        public void Save(GameSession session)
        {
            SaveCount++;
            _session = session;
        }
    }
}
