using MyTurn.Domain;

namespace MyTurn.Application;

public interface ISkillDefinitionRegistry
{
    IReadOnlyCollection<SkillDefinition> Definitions { get; }
}
