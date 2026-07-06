namespace MyTurn.Domain;

public interface IEquipmentItem : IItemDefinition
{
    EquipmentSlot Slot { get; }
    IReadOnlyCollection<StatModifierDefinition> StatModifiers { get; }
}
