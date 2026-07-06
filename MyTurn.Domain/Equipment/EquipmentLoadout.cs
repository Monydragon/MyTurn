namespace MyTurn.Domain;

public sealed class EquipmentLoadout
{
    private readonly Dictionary<EquipmentSlot, IEquipmentItem> _equippedItems = [];

    public IReadOnlyDictionary<EquipmentSlot, IEquipmentItem> EquippedItems => _equippedItems;
    public IWeapon EquippedWeapon => _equippedItems.TryGetValue(EquipmentSlot.Weapon, out var weapon) && weapon is IWeapon equippedWeapon
        ? equippedWeapon
        : throw new InvalidOperationException("No weapon is currently equipped.");

    public EquipmentLoadout(IWeapon equippedWeapon)
    {
        Equip(equippedWeapon);
    }

    public IEquipmentItem? this[EquipmentSlot slot] => _equippedItems.GetValueOrDefault(slot);

    public IEquipmentItem? Equip(IEquipmentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var previous = _equippedItems.GetValueOrDefault(item.Slot);
        _equippedItems[item.Slot] = item;

        return previous;
    }

    public IEquipmentItem? Unequip(EquipmentSlot slot)
    {
        var previous = _equippedItems.GetValueOrDefault(slot);
        _equippedItems.Remove(slot);

        return previous;
    }

    public void EquipWeapon(IWeapon weapon)
    {
        Equip(weapon);
    }
}
