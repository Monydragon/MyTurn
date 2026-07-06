using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class DefaultEnemyDefinitionRegistry : IEnemyDefinitionRegistry
{
    public IReadOnlyCollection<WeightedEnemyDefinition> Definitions => DefaultCatalogData.Enemies;
}
