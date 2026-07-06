using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class DefaultEnemyDefinitionRegistry : IEnemyDefinitionRegistry
{
    private static readonly WeightedEnemyDefinition[] DefaultDefinitions =
    [
        new(
            new EnemyDefinition(
                "cave-rat",
                "Cave Rat",
                new Dictionary<StatType, int>
                {
                    [StatType.Health] = 24,
                    [StatType.MeleeAttack] = 2,
                    [StatType.MeleeDefense] = 1,
                    [StatType.RangedDefense] = 1,
                    [StatType.MagicDefense] = 0,
                    [StatType.CriticalChance] = 2,
                    [StatType.Speed] = 12
                },
                new WeaponDefinition("rat-bite", "Rat Bite", WeaponType.Melee, 1, 4, []),
                20,
                [new WeightedEnemyAction(EnemyActionType.BasicAttack, 90), new WeightedEnemyAction(EnemyActionType.Defend, 10)],
                [new LootDropDefinition(LootDropKind.Currency, "currency", 4, 9, 70), new LootDropDefinition(LootDropKind.Item, "torn-cloth", 1, 2, 30)]),
            45),
        new(
            new EnemyDefinition(
                "goblin-scout",
                "Goblin Scout",
                new Dictionary<StatType, int>
                {
                    [StatType.Health] = 32,
                    [StatType.MeleeAttack] = 3,
                    [StatType.MeleeDefense] = 2,
                    [StatType.RangedAttack] = 2,
                    [StatType.RangedDefense] = 2,
                    [StatType.MagicDefense] = 1,
                    [StatType.CriticalChance] = 4,
                    [StatType.Speed] = 10
                },
                new WeaponDefinition("rusty-dagger", "Rusty Dagger", WeaponType.Melee, 2, 5, []),
                35,
                [new WeightedEnemyAction(EnemyActionType.BasicAttack, 80), new WeightedEnemyAction(EnemyActionType.Defend, 20)],
                [new LootDropDefinition(LootDropKind.Currency, "currency", 8, 15, 55), new LootDropDefinition(LootDropKind.Item, "goblin-ear", 1, 1, 25), new LootDropDefinition(LootDropKind.Item, "small-healing-potion", 1, 1, 20)]),
            35),
        new(
            new EnemyDefinition(
                "bandit-initiate",
                "Bandit Initiate",
                new Dictionary<StatType, int>
                {
                    [StatType.Health] = 40,
                    [StatType.MeleeAttack] = 4,
                    [StatType.MeleeDefense] = 3,
                    [StatType.RangedAttack] = 3,
                    [StatType.RangedDefense] = 3,
                    [StatType.MagicDefense] = 2,
                    [StatType.CriticalChance] = 5,
                    [StatType.Speed] = 9
                },
                new WeaponDefinition("chipped-sword", "Chipped Sword", WeaponType.Melee, 3, 6, []),
                50,
                [new WeightedEnemyAction(EnemyActionType.BasicAttack, 75), new WeightedEnemyAction(EnemyActionType.Defend, 25)],
                [new LootDropDefinition(LootDropKind.Currency, "currency", 12, 24, 60), new LootDropDefinition(LootDropKind.Item, "small-healing-potion", 1, 1, 20), new LootDropDefinition(LootDropKind.Item, "scout-boots", 1, 1, 20)]),
            20)
    ];

    public IReadOnlyCollection<WeightedEnemyDefinition> Definitions => DefaultDefinitions;
}
