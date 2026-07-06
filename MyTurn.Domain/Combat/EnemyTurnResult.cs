namespace MyTurn.Domain;

public sealed record EnemyTurnResult(
    Combatant Enemy,
    EnemyActionType ActionType,
    DamageResult? DamageResult);
