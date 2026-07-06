using System.Diagnostics.CodeAnalysis;
using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class DefaultWeaponDefinitionRegistry : IWeaponDefinitionRegistry
{
    public IReadOnlyCollection<WeaponDefinition> Definitions => DefaultCatalogData.Weapons;

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
