namespace MyTurn.Domain;

public sealed record StatModifierDefinition(
    StatType StatType,
    int Value);
