using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.Application;

[TestFixture]
public sealed class DefaultActorFactoryTests
{
    [Test]
    public void Create_BuildsActorWithRequestedIdentity()
    {
        var services = ApplicationServices.CreateDefault();
        var request = new CreateActorRequest("Avery", 24, Gender.NonBinary, Species.Human, CharacterClass.Mage);

        var actor = services.ActorFactory.Create(request);

        Assert.Multiple(() =>
        {
            Assert.That(actor.Name, Is.EqualTo("Avery"));
            Assert.That(actor.Age, Is.EqualTo(24));
            Assert.That(actor.Gender, Is.EqualTo(Gender.NonBinary));
            Assert.That(actor.Species, Is.EqualTo(Species.Human));
            Assert.That(actor.CharacterClass, Is.EqualTo(CharacterClass.Mage));
        });
    }

    [Test]
    public void Create_AddsDefaultStatsAndSkills()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Elf, CharacterClass.Archer));

        Assert.Multiple(() =>
        {
            Assert.That(actor.Skills.Count, Is.EqualTo(Enum.GetValues<SkillType>().Length));
            Assert.That(actor.Stats.Count, Is.EqualTo(Enum.GetValues<StatType>().Length));
            Assert.That(actor.Skills[SkillType.Melee].Leveling.CurrentLevel, Is.EqualTo(1));
            Assert.That(actor.Stats[StatType.Health].CurrentValue, Is.EqualTo(100));
            Assert.That(actor.Stats[StatType.Health].MaxValue, Is.EqualTo(100));
            Assert.That(actor.Steps, Is.EqualTo(0));
            Assert.That(actor.Equipment.EquippedWeapon.WeaponType, Is.EqualTo(WeaponType.Ranged));
        });
    }

    [Test]
    public void Create_RejectsInvalidName()
    {
        var services = ApplicationServices.CreateDefault();
        var request = new CreateActorRequest(" ", 24, Gender.Other, Species.Elf, CharacterClass.Archer);

        Assert.That(() => services.ActorFactory.Create(request), Throws.ArgumentException);
    }

    [Test]
    public void Create_RejectsUnderageActor()
    {
        var services = ApplicationServices.CreateDefault();
        var request = new CreateActorRequest("Avery", 17, Gender.Other, Species.Elf, CharacterClass.Archer);

        Assert.That(() => services.ActorFactory.Create(request), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
