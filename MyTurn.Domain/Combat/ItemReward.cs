namespace MyTurn.Domain;

public sealed record ItemReward(
    IItemDefinition Item,
    int Quantity);
