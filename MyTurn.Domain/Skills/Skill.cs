namespace MyTurn.Domain;

public sealed class Skill
{
    public SkillType SkillType { get; }
    public LevelContainer Leveling { get; }

    public Skill(SkillType skillType, LevelContainer leveling)
    {
        SkillType = skillType;
        Leveling = leveling;
    }
}
