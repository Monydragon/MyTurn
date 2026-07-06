using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class StartingEquipmentService : IStartingEquipmentService
{
    private readonly IWeaponDefinitionRegistry _weaponDefinitions;

    public StartingEquipmentService(IWeaponDefinitionRegistry weaponDefinitions)
    {
        _weaponDefinitions = weaponDefinitions;
    }

    public EquipmentLoadout CreateStartingLoadout(CharacterClass characterClass)
    {
        var weapon = _weaponDefinitions.Get(GetStartingWeaponType(characterClass));
        var loadout = new EquipmentLoadout(weapon);

        return loadout;
    }

    public WeaponType GetStartingWeaponType(CharacterClass characterClass)
    {
        return characterClass switch
        {
            CharacterClass.Warrior => WeaponType.Melee,
            CharacterClass.Archer => WeaponType.Ranged,
            CharacterClass.Mage => WeaponType.Magic,
            _ => throw new ArgumentOutOfRangeException(nameof(characterClass), characterClass, "Unsupported character class.")
        };
    }
}
