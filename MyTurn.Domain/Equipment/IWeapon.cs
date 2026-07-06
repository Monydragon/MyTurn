namespace MyTurn.Domain;

public interface IWeapon : IEquipmentItem
{
    WeaponType WeaponType { get; }
    int MinDamage { get; }
    int MaxDamage { get; }
}
