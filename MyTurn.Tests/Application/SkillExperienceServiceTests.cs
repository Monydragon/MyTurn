using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.Application;

[TestFixture]
public sealed class SkillExperienceServiceTests
{
    [Test]
    public void AddExperience_UpdatesSelectedSkillThroughIndexer()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Human, CharacterClass.Warrior));

        services.SkillExperienceService.AddExperience(actor, SkillType.Melee, 100);

        Assert.Multiple(() =>
        {
            Assert.That(actor.Skills[SkillType.Melee].Leveling.CurrentLevel, Is.EqualTo(2));
            Assert.That(actor.Skills[SkillType.Ranged].Leveling.CurrentLevel, Is.EqualTo(1));
        });
    }

    [Test]
    public void SkillSet_TryGet_ReturnsRegisteredSkill()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Human, CharacterClass.Warrior));

        var found = actor.Skills.TryGet(SkillType.Magic, out var skill);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(skill, Is.Not.Null);
            Assert.That(skill!.SkillType, Is.EqualTo(SkillType.Magic));
        });
    }

    [Test]
    public void StatSet_TryGet_ReturnsRegisteredStat()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Human, CharacterClass.Warrior));

        var found = actor.Stats.TryGet(StatType.Health, out var stat);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(stat, Is.Not.Null);
            Assert.That(stat!.StatType, Is.EqualTo(StatType.Health));
        });
    }
}
