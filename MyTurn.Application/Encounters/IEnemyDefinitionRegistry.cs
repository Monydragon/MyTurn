namespace MyTurn.Application;

public interface IEnemyDefinitionRegistry
{
    IReadOnlyCollection<WeightedEnemyDefinition> Definitions { get; }
}
