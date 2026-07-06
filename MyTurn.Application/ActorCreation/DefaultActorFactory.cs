using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class DefaultActorFactory : IActorFactory
{
    private readonly ISkillDefinitionRegistry _skillDefinitions;
    private readonly IStatDefinitionRegistry _statDefinitions;
    private readonly IStartingEquipmentService _startingEquipmentService;
    private readonly IInventoryService _inventoryService;
    private readonly ICharacterCreationValidator _validator;

    public DefaultActorFactory(
        ISkillDefinitionRegistry skillDefinitions,
        IStatDefinitionRegistry statDefinitions,
        IStartingEquipmentService startingEquipmentService,
        IInventoryService inventoryService,
        ICharacterCreationValidator validator)
    {
        _skillDefinitions = skillDefinitions;
        _statDefinitions = statDefinitions;
        _startingEquipmentService = startingEquipmentService;
        _inventoryService = inventoryService;
        _validator = validator;
    }

    public Actor Create(CreateActorRequest request)
    {
        _validator.Validate(request);

        var actor = new Actor(
            request.Name,
            request.Age,
            request.Gender,
            request.Species,
            request.CharacterClass,
            CreateSkills(),
            CreateStats(),
            _startingEquipmentService.CreateStartingLoadout(request.CharacterClass),
            _inventoryService.CreateStartingInventory(request.CharacterClass));

        ApplyStartingEquipmentModifiers(actor);

        return actor;
    }

    private SkillSet CreateSkills()
    {
        return new SkillSet(_skillDefinitions.Definitions.Select(definition =>
            new Skill(
                definition.SkillType,
                new LevelContainer(
                    definition.Name,
                    definition.StartingLevel,
                    definition.StartingExperience,
                    definition.MaxLevel))));
    }

    private StatSet CreateStats()
    {
        return new StatSet(_statDefinitions.Definitions.Select(definition =>
            new Stat(definition.StatType, definition.BaseValue, definition.MaxValue)));
    }

    private static void ApplyStartingEquipmentModifiers(Actor actor)
    {
        foreach (var item in actor.Equipment.EquippedItems.Values)
        {
            var sourceId = EquipmentService.GetEquipmentSourceId(item.Slot);
            actor.Stats.ApplyModifiers(item.StatModifiers.Select(modifier =>
                new StatModifier(modifier.StatType, modifier.Value, sourceId)));
        }
    }
}
