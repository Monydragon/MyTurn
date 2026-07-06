using System.Diagnostics.CodeAnalysis;
using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class DefaultWeaponDefinitionRegistry : IWeaponDefinitionRegistry
{
    private static readonly WeaponDefinition[] DefaultDefinitions =
    [
        new("training-sword", "Training Sword", WeaponType.Melee, 3, 6, [new StatModifierDefinition(StatType.MeleeAttack, 1)]),
        new("training-bow", "Training Bow", WeaponType.Ranged, 2, 7, [new StatModifierDefinition(StatType.RangedAttack, 1)]),
        new("apprentice-wand", "Apprentice Wand", WeaponType.Magic, 2, 8, [new StatModifierDefinition(StatType.MagicAttack, 1)])
    ];

    public IReadOnlyCollection<WeaponDefinition> Definitions => DefaultDefinitions;

    public WeaponDefinition Get(WeaponType weaponType)
    {
        return TryGet(weaponType, out var weapon)
            ? weapon
            : throw new KeyNotFoundException($"Weapon type '{weaponType}' is not registered.");
    }

    public bool TryGet(WeaponType weaponType, [NotNullWhen(true)] out WeaponDefinition? weapon)
    {
        weapon = Definitions.FirstOrDefault(definition => definition.WeaponType == weaponType);
        return weapon is not null;
    }
}
