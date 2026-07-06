using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class DefaultSkillDefinitionRegistry : ISkillDefinitionRegistry
{
    private static readonly SkillDefinition[] DefaultDefinitions = Enum.GetValues<SkillType>()
        .Select(skillType => new SkillDefinition(skillType, skillType.GetDisplayName()))
        .ToArray();

    public IReadOnlyCollection<SkillDefinition> Definitions => DefaultDefinitions;
}
