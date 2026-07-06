namespace MyTurn.Application;

public sealed class ApplicationServices
{
    public IActorFactory ActorFactory { get; }
    public ICharacterCreationValidator CharacterCreationValidator { get; }
    public ICombatService CombatService { get; }
    public IEncounterGenerator EncounterGenerator { get; }
    public IEnemyDefinitionRegistry EnemyDefinitions { get; }
    public IEquipmentService EquipmentService { get; }
    public IExplorationService ExplorationService { get; }
    public IGameFlowService GameFlowService { get; }
    public IInventoryService InventoryService { get; }
    public IItemDefinitionRegistry ItemDefinitions { get; }
    public ILootService LootService { get; }
    public IMinimapService MinimapService { get; }
    public IPartyService PartyService { get; }
    public IQuickStartPartyFactory QuickStartPartyFactory { get; }
    public IRecruitmentService RecruitmentService { get; }
    public IEncounterDifficultyService EncounterDifficultyService { get; }
    public ISkillExperienceService SkillExperienceService { get; }
    public IStartingEquipmentService StartingEquipmentService { get; }
    public ITreasureLootService TreasureLootService { get; }
    public IWeaponDefinitionRegistry WeaponDefinitions { get; }
    public IWorldGenerator WorldGenerator { get; }
    public IWorldSessionService WorldSessionService { get; }
    public ISkillDefinitionRegistry SkillDefinitions { get; }
    public IStatDefinitionRegistry StatDefinitions { get; }
    public IGamePersistenceService? GamePersistence { get; }

    public ApplicationServices(
        IActorFactory actorFactory,
        ICharacterCreationValidator characterCreationValidator,
        ICombatService combatService,
        IEncounterGenerator encounterGenerator,
        IEnemyDefinitionRegistry enemyDefinitions,
        IEquipmentService equipmentService,
        IExplorationService explorationService,
        IGameFlowService gameFlowService,
        IInventoryService inventoryService,
        IItemDefinitionRegistry itemDefinitions,
        ILootService lootService,
        IMinimapService minimapService,
        IPartyService partyService,
        IQuickStartPartyFactory quickStartPartyFactory,
        IRecruitmentService recruitmentService,
        IEncounterDifficultyService encounterDifficultyService,
        ISkillExperienceService skillExperienceService,
        IStartingEquipmentService startingEquipmentService,
        ITreasureLootService treasureLootService,
        IWeaponDefinitionRegistry weaponDefinitions,
        IWorldGenerator worldGenerator,
        IWorldSessionService worldSessionService,
        ISkillDefinitionRegistry skillDefinitions,
        IStatDefinitionRegistry statDefinitions,
        IGamePersistenceService? gamePersistence = null)
    {
        ActorFactory = actorFactory;
        CharacterCreationValidator = characterCreationValidator;
        CombatService = combatService;
        EncounterGenerator = encounterGenerator;
        EnemyDefinitions = enemyDefinitions;
        EquipmentService = equipmentService;
        ExplorationService = explorationService;
        GameFlowService = gameFlowService;
        InventoryService = inventoryService;
        ItemDefinitions = itemDefinitions;
        LootService = lootService;
        MinimapService = minimapService;
        PartyService = partyService;
        QuickStartPartyFactory = quickStartPartyFactory;
        RecruitmentService = recruitmentService;
        EncounterDifficultyService = encounterDifficultyService;
        SkillExperienceService = skillExperienceService;
        StartingEquipmentService = startingEquipmentService;
        TreasureLootService = treasureLootService;
        WeaponDefinitions = weaponDefinitions;
        WorldGenerator = worldGenerator;
        WorldSessionService = worldSessionService;
        SkillDefinitions = skillDefinitions;
        StatDefinitions = statDefinitions;
        GamePersistence = gamePersistence;
    }

    public static ApplicationServices CreateDefault()
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
            statDefinitions);
    }
}
