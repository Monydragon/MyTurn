using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class SkillExperienceService : ISkillExperienceService
{
    public Skill AddExperience(Actor actor, SkillType skillType, int experience)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var skill = actor.Skills[skillType];
        skill.Leveling.AddExperience(experience);

        return skill;
    }
}
