using System.Diagnostics.CodeAnalysis;
using MyTurn.Domain;

namespace MyTurn.Application;

public interface IWeaponDefinitionRegistry
{
    IReadOnlyCollection<WeaponDefinition> Definitions { get; }
    WeaponDefinition Get(WeaponType weaponType);
    bool TryGet(WeaponType weaponType, [NotNullWhen(true)] out WeaponDefinition? weapon);
}
