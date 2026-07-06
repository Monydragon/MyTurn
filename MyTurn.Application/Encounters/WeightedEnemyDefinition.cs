using MyTurn.Domain;

namespace MyTurn.Application;

public sealed record WeightedEnemyDefinition(
    EnemyDefinition Enemy,
    int Weight);
