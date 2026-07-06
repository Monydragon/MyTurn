namespace MyTurn.Domain;

public sealed record ArmorDefinition(
    string Id,
    string Name,
    EquipmentSlot Slot,
    IReadOnlyCollection<StatModifierDefinition> StatModifiers) : IEquipmentItem
{
    public ItemKind Kind => ItemKind.Armor;
    public bool IsStackable => false;
}
