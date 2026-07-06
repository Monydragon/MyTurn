namespace MyTurn.Domain;

public sealed record ConsumableDefinition(
    string Id,
    string Name,
    int HealingAmount) : IItemDefinition
{
    public ItemKind Kind => ItemKind.Consumable;
    public bool IsStackable => true;
}
