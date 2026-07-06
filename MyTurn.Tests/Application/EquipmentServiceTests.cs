using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.Application;

[TestFixture]
public sealed class EquipmentServiceTests
{
    [TestCase(CharacterClass.Warrior, WeaponType.Melee)]
    [TestCase(CharacterClass.Archer, WeaponType.Ranged)]
    [TestCase(CharacterClass.Mage, WeaponType.Magic)]
    public void Create_AssignsStartingWeaponFromCharacterClass(CharacterClass characterClass, WeaponType expectedWeaponType)
    {
        var services = ApplicationServices.CreateDefault();
        var actor = services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Human, characterClass));

        Assert.That(actor.Equipment.EquippedWeapon.WeaponType, Is.EqualTo(expectedWeaponType));
    }

    [Test]
    public void ChangeEquippedWeapon_UpdatesLoadoutWithoutChangingStatsSkillsOrSteps()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Human, CharacterClass.Warrior));
        var health = actor.Stats[StatType.Health].CurrentValue;
        var meleeLevel = actor.Skills[SkillType.Melee].Leveling.CurrentLevel;
        actor.AddSteps(42);

        var weapon = services.EquipmentService.ChangeEquippedWeapon(actor, WeaponType.Magic);

        Assert.Multiple(() =>
        {
            Assert.That(weapon.WeaponType, Is.EqualTo(WeaponType.Magic));
            Assert.That(actor.Equipment.EquippedWeapon.WeaponType, Is.EqualTo(WeaponType.Magic));
            Assert.That(actor.Stats[StatType.Health].CurrentValue, Is.EqualTo(health));
            Assert.That(actor.Skills[SkillType.Melee].Leveling.CurrentLevel, Is.EqualTo(meleeLevel));
            Assert.That(actor.Steps, Is.EqualTo(42));
        });
    }
}
