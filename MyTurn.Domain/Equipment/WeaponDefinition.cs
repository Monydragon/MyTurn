namespace MyTurn.Domain;

public sealed record WeaponDefinition(
    string Id,
    string Name,
    WeaponType WeaponType,
    int MinDamage,
    int MaxDamage,
    IReadOnlyCollection<StatModifierDefinition> StatModifiers) : IWeapon
{
    public ItemKind Kind => ItemKind.Weapon;
    public bool IsStackable => false;
    public EquipmentSlot Slot => EquipmentSlot.Weapon;

    public WeaponDefinition(string name, WeaponType weaponType)
        : this(
            name.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).ToLowerInvariant(),
            name,
            weaponType,
            2,
            4,
            [])
    {
    }
}
