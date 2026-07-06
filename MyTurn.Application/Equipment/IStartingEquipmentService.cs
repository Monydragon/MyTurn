using MyTurn.Domain;

namespace MyTurn.Application;

public interface IStartingEquipmentService
{
    EquipmentLoadout CreateStartingLoadout(CharacterClass characterClass);
    WeaponType GetStartingWeaponType(CharacterClass characterClass);
}
