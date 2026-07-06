namespace MyTurn.Domain;

public sealed record WeightedEnemyAction(
    EnemyActionType ActionType,
    int Weight);
