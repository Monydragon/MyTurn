namespace MyTurn.Domain;

public sealed record HealingResult(
    Combatant Target,
    ConsumableDefinition Consumable,
    int AmountHealed);
