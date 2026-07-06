using MyTurn.Application;

namespace MyTurn.Infrastructure.Catalog;

internal sealed class SqliteEnemyDefinitionRegistry : IEnemyDefinitionRegistry
{
    public SqliteEnemyDefinitionRegistry(IEnumerable<WeightedEnemyDefinition> definitions)
    {
        Definitions = definitions.ToArray();
    }

    public IReadOnlyCollection<WeightedEnemyDefinition> Definitions { get; }
}
