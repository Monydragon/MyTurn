using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class DefaultStatDefinitionRegistry : IStatDefinitionRegistry
{
    private static readonly StatDefinition[] DefaultDefinitions =
    [
        new(StatType.Health, StatType.Health.GetDisplayName(), 100, 100),
        new(StatType.MeleeAttack, StatType.MeleeAttack.GetDisplayName(), 1, 1),
        new(StatType.MeleeDefense, StatType.MeleeDefense.GetDisplayName(), 1, 1),
        new(StatType.RangedAttack, StatType.RangedAttack.GetDisplayName(), 1, 1),
        new(StatType.RangedDefense, StatType.RangedDefense.GetDisplayName(), 1, 1),
        new(StatType.MagicAttack, StatType.MagicAttack.GetDisplayName(), 1, 1),
        new(StatType.MagicDefense, StatType.MagicDefense.GetDisplayName(), 1, 1),
        new(StatType.CriticalChance, StatType.CriticalChance.GetDisplayName(), 1, 1),
        new(StatType.Speed, StatType.Speed.GetDisplayName(), 10, 10)
    ];

    public IReadOnlyCollection<StatDefinition> Definitions => DefaultDefinitions;
}
