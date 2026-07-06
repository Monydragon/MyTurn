using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace MyTurn.Domain;

public sealed class SkillSet : IReadOnlyCollection<Skill>
{
    private readonly IReadOnlyList<Skill> _orderedSkills;
    private readonly Dictionary<SkillType, Skill> _skills;

    public SkillSet(IEnumerable<Skill> skills)
    {
        _orderedSkills = skills.ToArray();
        _skills = _orderedSkills.ToDictionary(skill => skill.SkillType);
    }

    public int Count => _skills.Count;

    public Skill this[SkillType skillType] => _skills.TryGetValue(skillType, out var skill)
        ? skill
        : throw new KeyNotFoundException($"Skill '{skillType}' is not registered.");

    public bool TryGet(SkillType skillType, [NotNullWhen(true)] out Skill? skill)
    {
        return _skills.TryGetValue(skillType, out skill);
    }

    public IEnumerator<Skill> GetEnumerator()
    {
        return _orderedSkills.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
