namespace MyTurn.Domain;

public sealed record CombatActionResolution(
    CombatActionType ActionType,
    bool ConsumesTurn);
