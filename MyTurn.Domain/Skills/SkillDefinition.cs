namespace MyTurn.Domain;

public sealed record SkillDefinition(
    SkillType SkillType,
    string Name,
    int StartingLevel = 1,
    int StartingExperience = 0,
    int MaxLevel = 100);
