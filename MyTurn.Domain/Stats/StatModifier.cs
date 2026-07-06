namespace MyTurn.Domain;

public sealed record StatModifier(
    StatType StatType,
    int Value,
    string SourceId);
