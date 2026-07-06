using MyTurn.Domain;

namespace MyTurn.Application;

public interface IEquipmentService
{
    IWeapon ChangeEquippedWeapon(Actor actor, WeaponType weaponType);
    IEquipmentItem Equip(Actor actor, IEquipmentItem item);
    IEquipmentItem? Unequip(Actor actor, EquipmentSlot slot);
}
