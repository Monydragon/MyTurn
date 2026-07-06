using System.Diagnostics.CodeAnalysis;
using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class DefaultItemDefinitionRegistry : IItemDefinitionRegistry
{
    private static readonly IItemDefinition[] DefaultDefinitions =
    [
        new ConsumableDefinition("small-healing-potion", "Small Healing Potion", 25),
        new MaterialDefinition("goblin-ear", "Goblin Ear"),
        new MaterialDefinition("torn-cloth", "Torn Cloth"),
        new ArmorDefinition("cloth-tunic", "Cloth Tunic", EquipmentSlot.Body, [new StatModifierDefinition(StatType.MeleeDefense, 1)]),
        new ArmorDefinition("scout-boots", "Scout Boots", EquipmentSlot.Feet, [new StatModifierDefinition(StatType.Speed, 1)])
    ];

    private readonly Dictionary<string, IItemDefinition> _definitions;

    public DefaultItemDefinitionRegistry(IWeaponDefinitionRegistry weaponDefinitions)
    {
        _definitions = weaponDefinitions.Definitions
            .Cast<IItemDefinition>()
            .Concat(DefaultDefinitions)
            .ToDictionary(definition => definition.Id);
    }

    public IReadOnlyCollection<IItemDefinition> Definitions => _definitions.Values.ToArray();

    public IItemDefinition Get(string itemId)
    {
        return TryGet(itemId, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Item '{itemId}' is not registered.");
    }

    public bool TryGet(string itemId, [NotNullWhen(true)] out IItemDefinition? definition)
    {
        return _definitions.TryGetValue(itemId, out definition);
    }
}
