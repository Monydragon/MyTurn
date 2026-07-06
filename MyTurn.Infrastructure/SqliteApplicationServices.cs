using Microsoft.EntityFrameworkCore;
using MyTurn.Application;
using MyTurn.Infrastructure.Catalog;
using MyTurn.Infrastructure.Data;
using MyTurn.Infrastructure.Persistence;
using MyTurn.Infrastructure.SeedData;

namespace MyTurn.Infrastructure;

public static class SqliteApplicationServices
{
    public static ApplicationServices CreateDefault()
    {
        return Create(GetDefaultDatabasePath(), migrateDatabase: true);
    }

    public static ApplicationServices Create(string databasePath, bool migrateDatabase = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var directory = Path.GetDirectoryName(databasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = CreateOptions(CreateConnectionString(databasePath));

        return Create(options, migrateDatabase);
    }

    public static ApplicationServices Create(DbContextOptions<MyTurnDbContext> options, bool migrateDatabase)
    {
        ArgumentNullException.ThrowIfNull(options);

        SQLitePCL.Batteries_V2.Init();

        using (var db = new MyTurnDbContext(options))
        {
            if (migrateDatabase)
            {
                db.Database.Migrate();
            }
            else
            {
                db.Database.EnsureCreated();
            }

            SqliteDatabaseSeeder.Seed(db);
        }

        CatalogSnapshot catalog;

        using (var db = new MyTurnDbContext(options))
        {
            catalog = SqliteCatalogLoader.Load(db);
        }

        var skillDefinitions = new DefaultSkillDefinitionRegistry();
        var statDefinitions = new DefaultStatDefinitionRegistry();
        var characterCreationValidator = new CharacterCreationValidator();
        var inventoryService = new InventoryService(catalog.ItemDefinitions);
        var startingEquipmentService = new StartingEquipmentService(catalog.WeaponDefinitions);
        var equipmentService = new EquipmentService(catalog.WeaponDefinitions);
        var lootService = new LootService(catalog.ItemDefinitions);
        var encounterGenerator = new EncounterGenerator(catalog.EnemyDefinitions);
        var combatService = new CombatService(equipmentService, catalog.ItemDefinitions, lootService, statDefinitions);
        var treasureLootService = new TreasureLootService(catalog.ItemDefinitions);
        var worldGenerator = new WorldGenerator();
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
        var persistence = new SqliteGamePersistenceService(
            options,
            catalog.ItemDefinitions,
            catalog.WeaponDefinitions,
            skillDefinitions,
            statDefinitions);

        return new ApplicationServices(
            actorFactory,
            characterCreationValidator,
            combatService,
            encounterGenerator,
            catalog.EnemyDefinitions,
            equipmentService,
            explorationService,
            gameFlowService,
            inventoryService,
            catalog.ItemDefinitions,
            lootService,
            minimapService,
            partyService,
            quickStartPartyFactory,
            recruitmentService,
            encounterDifficultyService,
            skillExperienceService,
            startingEquipmentService,
            treasureLootService,
            catalog.WeaponDefinitions,
            worldGenerator,
            worldSessionService,
            skillDefinitions,
            statDefinitions,
            persistence);
    }

    public static DbContextOptions<MyTurnDbContext> CreateOptions(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new DbContextOptionsBuilder<MyTurnDbContext>()
            .UseSqlite(connectionString)
            .Options;
    }

    public static string CreateConnectionString(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        return $"Data Source={databasePath};Pooling=False";
    }

    public static string GetDefaultDatabasePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = AppContext.BaseDirectory;
        }

        return Path.Combine(localAppData, "MyTurn", "myturn.db");
    }
}
