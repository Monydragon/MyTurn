using System.Diagnostics.CodeAnalysis;
using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Infrastructure.Catalog;

internal sealed class SqliteItemDefinitionRegistry : IItemDefinitionRegistry
{
    private readonly Dictionary<string, IItemDefinition> _definitions;

    public SqliteItemDefinitionRegistry(IEnumerable<IItemDefinition> definitions)
    {
        _definitions = definitions.ToDictionary(definition => definition.Id);
    }

    public IReadOnlyCollection<IItemDefinition> Definitions => _definitions.Values.ToArray();

    public IItemDefinition Get(string itemId)
    {
        return TryGet(itemId, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Item '{itemId}' is not registered.");
    }

    public bool TryGet(string itemId, [NotNullWhen(true)] out IItemDefinition? definition)
    {
        return _definitions.TryGetValue(itemId, out definition);
    }
}
