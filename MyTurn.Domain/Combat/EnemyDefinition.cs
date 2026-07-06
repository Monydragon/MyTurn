namespace MyTurn.Domain;

public sealed record EnemyDefinition(
    string Id,
    string Name,
    IReadOnlyDictionary<StatType, int> Stats,
    IWeapon Weapon,
    int ExperienceReward,
    IReadOnlyCollection<WeightedEnemyAction> Actions,
    IReadOnlyCollection<LootDropDefinition> LootTable);
