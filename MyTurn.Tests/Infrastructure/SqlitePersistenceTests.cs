using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Infrastructure;
using MyTurn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MyTurn.Tests.Infrastructure;

[TestFixture]
public sealed class SqlitePersistenceTests
{
    private string _databasePath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"myturn-{Guid.NewGuid():N}.db");
    }

    [TearDown]
    public void TearDown()
    {
        DeleteIfExists(_databasePath);
        DeleteIfExists($"{_databasePath}-shm");
        DeleteIfExists($"{_databasePath}-wal");
    }

    [Test]
    public void Create_RunsMigrationsAndSeedsCatalogsIdempotently()
    {
        SqliteApplicationServices.Create(_databasePath);
        var firstCounts = GetCatalogCounts();

        SqliteApplicationServices.Create(_databasePath);
        var secondCounts = GetCatalogCounts();

        Assert.Multiple(() =>
        {
            Assert.That(firstCounts.Items, Is.GreaterThanOrEqualTo(8));
            Assert.That(firstCounts.Enemies, Is.EqualTo(3));
            Assert.That(firstCounts.ItemModifiers, Is.GreaterThanOrEqualTo(3));
            Assert.That(secondCounts, Is.EqualTo(firstCounts));
        });
    }

    [Test]
    public void Create_LoadsDatabaseBackedCatalogs()
    {
        var services = SqliteApplicationServices.Create(_databasePath);

        var potion = services.ItemDefinitions.Get("small-healing-potion");
        var weapon = services.WeaponDefinitions.Get(WeaponType.Melee);
        var enemies = services.EnemyDefinitions.Definitions;

        Assert.Multiple(() =>
        {
            Assert.That(potion, Is.TypeOf<ConsumableDefinition>());
            Assert.That(weapon.Id, Is.EqualTo("training-sword"));
            Assert.That(enemies.Select(enemy => enemy.Enemy.Id), Does.Contain("goblin-scout"));
            Assert.That(enemies.Single(enemy => enemy.Enemy.Id == "bandit-initiate").Enemy.ThreatRating, Is.EqualTo(3));
        });
    }

    [Test]
    public void SaveAndLoad_PreservesPlayerProgress()
    {
        var services = SqliteApplicationServices.Create(_databasePath);
        var actor = CreateActor(services);
        var boots = (IEquipmentItem)services.ItemDefinitions.Get("scout-boots");

        actor.Inventory.AddCurrency(42);
        actor.Inventory.Add(boots);
        actor.Skills[SkillType.Melee].Leveling.AddExperience(30);
        actor.AddSteps(7);
        services.EquipmentService.Equip(actor, boots);

        var created = services.GamePersistence!.CreateSave(actor);
        var loaded = services.GamePersistence.LoadSave(created.SaveSlotId);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Actor.Id, Is.EqualTo(actor.Id));
            Assert.That(loaded.Actor.Name, Is.EqualTo("Avery"));
            Assert.That(loaded.Actor.Inventory.Currency, Is.EqualTo(42));
            Assert.That(loaded.Actor.Inventory.GetQuantity("scout-boots"), Is.EqualTo(1));
            Assert.That(loaded.Actor.Equipment[EquipmentSlot.Feet]?.Id, Is.EqualTo("scout-boots"));
            Assert.That(loaded.Actor.Skills[SkillType.Melee].Leveling.Experience, Is.EqualTo(30));
            Assert.That(loaded.Actor.Steps, Is.EqualTo(7));
            Assert.That(loaded.Actor.Stats[StatType.Speed].CurrentValue, Is.EqualTo(11));
        });
    }

    [Test]
    public void SaveAndLoad_PreservesGeneratedWorldState()
    {
        var services = SqliteApplicationServices.Create(_databasePath);
        var actor = CreateActor(services);
        var gameSession = services.GamePersistence!.CreateSave(actor);
        var world = services.WorldSessionService.CreateNew(gameSession.Party, seed: 1234);

        services.ExplorationService.TryMove(gameSession.Party, world, Direction.East);

        if (world.CurrentRoom.RoomType == RoomType.Enemy)
        {
            services.ExplorationService.ClearEnemyRoom(world);
        }

        gameSession = gameSession with { ActiveWorldSession = world };
        services.GamePersistence.Save(gameSession);

        var loaded = services.GamePersistence.LoadSave(gameSession.SaveSlotId);
        var loadedWorld = loaded.ActiveWorldSession!;
        var loadedRoom = loadedWorld.Map.GetRoom(world.CurrentPosition);

        Assert.Multiple(() =>
        {
            Assert.That(loadedWorld.Id, Is.EqualTo(world.Id));
            Assert.That(loadedWorld.Map.Seed, Is.EqualTo(1234));
            Assert.That(loadedWorld.CurrentPosition, Is.EqualTo(world.CurrentPosition));
            Assert.That(loadedWorld.Map.Rooms.Count, Is.EqualTo(world.Map.Rooms.Count));
            Assert.That(loadedRoom.IsVisited, Is.True);
            Assert.That(
                loadedWorld.Map.Rooms.Values.Where(room => room.RoomType == RoomType.Enemy),
                Has.All.Matches<WorldRoom>(room => room.EncounterSeed.HasValue));
        });
    }

    [Test]
    public void Save_ReloadsLatestProgressAfterRewardGearTreasureAndMovement()
    {
        var services = SqliteApplicationServices.Create(_databasePath);
        var actor = CreateActor(services);
        var gameSession = services.GamePersistence!.CreateSave(actor);
        var tunic = (IEquipmentItem)services.ItemDefinitions.Get("cloth-tunic");
        var world = services.WorldSessionService.CreateNew(gameSession.Party, seed: FindSeedWithAdjacentTreasure());

        gameSession.Party.Inventory.AddCurrency(10);
        gameSession.Party.Inventory.Add(tunic);
        services.EquipmentService.Equip(actor, tunic);
        services.ExplorationService.TryMove(gameSession.Party, world, Direction.East);

        gameSession = gameSession with { ActiveWorldSession = world };
        services.GamePersistence.Save(gameSession);

        var loaded = services.GamePersistence.LoadSave(gameSession.SaveSlotId);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Actor.Inventory.Currency, Is.GreaterThanOrEqualTo(10));
            Assert.That(loaded.Actor.Equipment[EquipmentSlot.Body]?.Id, Is.EqualTo("cloth-tunic"));
            Assert.That(loaded.Actor.Steps, Is.EqualTo(1));
            Assert.That(loaded.ActiveWorldSession?.CurrentPosition, Is.EqualTo(new WorldPosition(1, 0)));
            Assert.That(loaded.ActiveWorldSession?.CurrentRoom.IsVisited, Is.True);
        });
    }

    [Test]
    public void SaveAndLoad_PreservesPartyRosterSharedInventoryAndReserveMembers()
    {
        var services = SqliteApplicationServices.Create(_databasePath);
        var activeMembers = new[]
        {
            CreateActor(services, "Avery", CharacterClass.Warrior),
            CreateActor(services, "Blake", CharacterClass.Archer),
            CreateActor(services, "Casey", CharacterClass.Mage),
            CreateActor(services, "Devon", CharacterClass.Warrior)
        };
        var reserve = CreateActor(services, "Emery", CharacterClass.Archer);
        var inventory = new Inventory();

        inventory.AddCurrency(99);
        inventory.Add(services.ItemDefinitions.Get("small-healing-potion"), 5);

        var party = new Party(activeMembers, [reserve], inventory);
        party.AddSteps(4);

        var created = services.GamePersistence!.CreateSave(party);
        var loaded = services.GamePersistence.LoadSave(created.SaveSlotId);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Party.ActiveMembers.Select(member => member.Name), Is.EqualTo(new[] { "Avery", "Blake", "Casey", "Devon" }));
            Assert.That(loaded.Party.ReserveMembers.Select(member => member.Name), Is.EqualTo(new[] { "Emery" }));
            Assert.That(loaded.Party.Inventory.Currency, Is.EqualTo(99));
            Assert.That(loaded.Party.Inventory.GetQuantity("small-healing-potion"), Is.EqualTo(5));
            Assert.That(loaded.Party.Steps, Is.EqualTo(4));
            Assert.That(loaded.Party.ActiveMembers, Has.All.Matches<Actor>(member => member.Steps == 4));
            Assert.That(loaded.Party.ReserveMembers.Single().Steps, Is.EqualTo(0));
        });
    }

    [Test]
    public void Migration_LoadsExistingSinglePlayerSaveAsOneMemberParty()
    {
        var options = SqliteApplicationServices.CreateOptions(SqliteApplicationServices.CreateConnectionString(_databasePath));
        var saveId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        using (var db = new MyTurnDbContext(options))
        {
            db.Database.Migrate("20260706190817_InitialCreate");
            db.Database.ExecuteSqlInterpolated($"""
                INSERT INTO SaveSlots (Id, Name, CreatedAtUtc, LastPlayedAtUtc)
                VALUES ({saveId}, {"Legacy Save"}, {now}, {now});
                """);
            db.Database.ExecuteSqlInterpolated($"""
                INSERT INTO Players (Id, SaveSlotId, Name, Age, Gender, Species, CharacterClass, Steps, Currency)
                VALUES ({playerId}, {saveId}, {"Legacy Hero"}, {28}, {"Other"}, {"Human"}, {"Warrior"}, {9L}, {25L});
                """);
        }

        var services = SqliteApplicationServices.Create(_databasePath);
        var loaded = services.GamePersistence!.LoadSave(saveId);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Party.ActiveMembers, Has.Count.EqualTo(1));
            Assert.That(loaded.Party.ReserveMembers, Is.Empty);
            Assert.That(loaded.Party.Leader.Id, Is.EqualTo(playerId));
            Assert.That(loaded.Party.Leader.Name, Is.EqualTo("Legacy Hero"));
            Assert.That(loaded.Party.Inventory.Currency, Is.EqualTo(25));
            Assert.That(loaded.Party.Steps, Is.EqualTo(9));
            Assert.That(loaded.Party.Leader.Steps, Is.EqualTo(9));
        });
    }

    private static Actor CreateActor(
        ApplicationServices services,
        string name = "Avery",
        CharacterClass characterClass = CharacterClass.Warrior)
    {
        return services.ActorFactory.Create(
            new CreateActorRequest(name, 24, Gender.Other, Species.Human, characterClass));
    }

    private (int Items, int ItemModifiers, int Enemies) GetCatalogCounts()
    {
        var options = SqliteApplicationServices.CreateOptions(SqliteApplicationServices.CreateConnectionString(_databasePath));

        using var db = new MyTurnDbContext(options);

        return (db.ItemDefinitions.Count(), db.ItemStatModifiers.Count(), db.EnemyDefinitions.Count());
    }

    private static int FindSeedWithAdjacentTreasure()
    {
        var generator = new WorldGenerator();

        for (var seed = 1; seed < 10_000; seed++)
        {
            var map = generator.Generate(new WorldGenerationRequest(seed));

            if (map.GetRoom(new WorldPosition(1, 0)).RoomType == RoomType.Treasure)
            {
                return seed;
            }
        }

        throw new InvalidOperationException("Could not find a deterministic test seed with adjacent treasure.");
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
