namespace MyTurn.Domain;

public sealed class Stat
{
    private const string ManualModifierSource = "manual";
    private readonly List<StatModifier> _modifiers;

    public StatType StatType { get; }
    public int BaseValue { get; private set; }
    public int CurrentValue => BaseValue + _modifiers.Sum(modifier => modifier.Value);
    public int MaxValue { get; private set; }
    public IReadOnlyList<int> Modifiers => _modifiers.Select(modifier => modifier.Value).ToArray();
    public IReadOnlyList<StatModifier> SourceModifiers => _modifiers;

    public Stat(StatType statType, int baseValue, int maxValue, IEnumerable<int>? modifiers = null)
    {
        StatType = statType;
        BaseValue = baseValue;
        MaxValue = maxValue;
        _modifiers = modifiers?
            .Select(modifier => new StatModifier(statType, modifier, ManualModifierSource))
            .ToList() ?? [];
    }

    public void SetBaseValue(int baseValue)
    {
        BaseValue = baseValue;
    }

    public void SetMaxValue(int maxValue)
    {
        MaxValue = maxValue;
    }

    public void AddModifiers(params int[] modifiers)
    {
        _modifiers.AddRange(modifiers.Select(modifier => new StatModifier(StatType, modifier, ManualModifierSource)));
    }

    public void AddModifier(int modifier)
    {
        _modifiers.Add(new StatModifier(StatType, modifier, ManualModifierSource));
    }

    public void AddModifier(StatModifier modifier)
    {
        if (modifier.StatType != StatType)
        {
            throw new ArgumentException("Modifier stat type must match the stat it is applied to.", nameof(modifier));
        }

        _modifiers.Add(modifier);
    }

    public void ClearModifiers()
    {
        _modifiers.Clear();
    }

    public void RemoveModifiers(params int[] modifiers)
    {
        foreach (var modifier in modifiers)
        {
            var matchingModifier = _modifiers.FirstOrDefault(current => current.Value == modifier);

            if (matchingModifier is not null)
            {
                _modifiers.Remove(matchingModifier);
            }
        }
    }

    public void RemoveModifier(int modifier)
    {
        RemoveModifiers(modifier);
    }

    public void RemoveModifiersBySource(string sourceId)
    {
        _modifiers.RemoveAll(modifier => modifier.SourceId == sourceId);
    }
}
