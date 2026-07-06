namespace MyTurn.Domain;

public sealed record StatDefinition(
    StatType StatType,
    string Name,
    int BaseValue,
    int MaxValue);
