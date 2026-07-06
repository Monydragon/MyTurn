using MyTurn.Domain;

namespace MyTurn.Application;

public interface ISkillExperienceService
{
    Skill AddExperience(Actor actor, SkillType skillType, int experience);
}
