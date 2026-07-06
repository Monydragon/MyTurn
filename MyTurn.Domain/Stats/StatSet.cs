using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace MyTurn.Domain;

public sealed class StatSet : IReadOnlyCollection<Stat>
{
    private readonly IReadOnlyList<Stat> _orderedStats;
    private readonly Dictionary<StatType, Stat> _stats;

    public StatSet(IEnumerable<Stat> stats)
    {
        _orderedStats = stats.ToArray();
        _stats = _orderedStats.ToDictionary(stat => stat.StatType);
    }

    public int Count => _stats.Count;

    public Stat this[StatType statType] => _stats.TryGetValue(statType, out var stat)
        ? stat
        : throw new KeyNotFoundException($"Stat '{statType}' is not registered.");

    public bool TryGet(StatType statType, [NotNullWhen(true)] out Stat? stat)
    {
        return _stats.TryGetValue(statType, out stat);
    }

    public void ApplyModifier(StatModifier modifier)
    {
        this[modifier.StatType].AddModifier(modifier);
    }

    public void ApplyModifiers(IEnumerable<StatModifier> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            ApplyModifier(modifier);
        }
    }

    public void RemoveModifiersBySource(string sourceId)
    {
        foreach (var stat in _orderedStats)
        {
            stat.RemoveModifiersBySource(sourceId);
        }
    }

    public IEnumerator<Stat> GetEnumerator()
    {
        return _orderedStats.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
