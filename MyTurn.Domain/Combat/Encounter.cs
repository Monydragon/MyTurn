namespace MyTurn.Domain;

public sealed record Encounter(
    int Seed,
    IReadOnlyList<EnemyDefinition> Enemies);
