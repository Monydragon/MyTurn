using System.Diagnostics.CodeAnalysis;
using MyTurn.Domain;

namespace MyTurn.Application;

public interface IItemDefinitionRegistry
{
    IReadOnlyCollection<IItemDefinition> Definitions { get; }
    IItemDefinition Get(string itemId);
    bool TryGet(string itemId, [NotNullWhen(true)] out IItemDefinition? definition);
}
