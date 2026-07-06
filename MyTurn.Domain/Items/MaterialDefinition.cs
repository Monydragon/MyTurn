namespace MyTurn.Domain;

public sealed record MaterialDefinition(
    string Id,
    string Name) : IItemDefinition
{
    public ItemKind Kind => ItemKind.Material;
    public bool IsStackable => true;
}
