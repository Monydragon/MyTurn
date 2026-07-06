namespace MyTurn.Domain;

public interface IItemDefinition
{
    string Id { get; }
    string Name { get; }
    ItemKind Kind { get; }
    bool IsStackable { get; }
}
