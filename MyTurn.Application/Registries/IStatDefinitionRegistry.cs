using MyTurn.Domain;

namespace MyTurn.Application;

public interface IStatDefinitionRegistry
{
    IReadOnlyCollection<StatDefinition> Definitions { get; }
}
