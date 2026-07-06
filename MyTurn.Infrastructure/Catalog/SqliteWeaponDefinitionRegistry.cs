using System.Diagnostics.CodeAnalysis;
using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Infrastructure.Catalog;

internal sealed class SqliteWeaponDefinitionRegistry : IWeaponDefinitionRegistry
{
    private static readonly IReadOnlyDictionary<WeaponType, string> StartingWeaponIds =
        new Dictionary<WeaponType, string>
        {
            [WeaponType.Melee] = "training-sword",
            [WeaponType.Ranged] = "training-bow",
            [WeaponType.Magic] = "apprentice-wand"
        };

    private readonly IReadOnlyList<WeaponDefinition> _definitions;

    public SqliteWeaponDefinitionRegistry(IEnumerable<WeaponDefinition> definitions)
    {
        _definitions = definitions.ToArray();
    }

    public IReadOnlyCollection<WeaponDefinition> Definitions => _definitions.ToArray();

    public WeaponDefinition Get(WeaponType weaponType)
    {
        return TryGet(weaponType, out var weapon)
            ? weapon
            : throw new KeyNotFoundException($"Weapon type '{weaponType}' is not registered.");
    }

    public bool TryGet(WeaponType weaponType, [NotNullWhen(true)] out WeaponDefinition? weapon)
    {
        weapon = StartingWeaponIds.TryGetValue(weaponType, out var startingWeaponId)
            ? _definitions.FirstOrDefault(definition => definition.Id == startingWeaponId)
            : null;

        weapon ??= _definitions.FirstOrDefault(definition => definition.WeaponType == weaponType);

        return weapon is not null;
    }
}
