using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.Combat;

[TestFixture]
public sealed class EquipmentModifierTests
{
    [Test]
    public void ActorCreation_AppliesStartingWeaponModifierAndIncludesSpeed()
    {
        var actor = CreateActor(CharacterClass.Warrior);

        Assert.Multiple(() =>
        {
            Assert.That(actor.Stats[StatType.Speed].CurrentValue, Is.EqualTo(10));
            Assert.That(actor.Stats[StatType.MeleeAttack].CurrentValue, Is.EqualTo(2));
        });
    }

    [Test]
    public void EquipAndUnequipGear_UpdatesSourceModifiersCleanly()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor(CharacterClass.Warrior);
        var tunic = (IEquipmentItem)services.ItemDefinitions.Get("cloth-tunic");

        services.EquipmentService.Equip(actor, tunic);
        services.EquipmentService.Unequip(actor, EquipmentSlot.Body);

        Assert.Multiple(() =>
        {
            Assert.That(actor.Stats[StatType.MeleeDefense].CurrentValue, Is.EqualTo(1));
            Assert.That(actor.Equipment[EquipmentSlot.Body], Is.Null);
        });
    }

    [Test]
    public void EquippingNewItemInSameSlot_ReplacesOldModifiers()
    {
        var services = ApplicationServices.CreateDefault();
        var actor = CreateActor(CharacterClass.Warrior);
        var boots = (IEquipmentItem)services.ItemDefinitions.Get("scout-boots");

        services.EquipmentService.Equip(actor, boots);
        services.EquipmentService.Equip(actor, boots);

        Assert.That(actor.Stats[StatType.Speed].CurrentValue, Is.EqualTo(11));
    }

    private static Actor CreateActor(CharacterClass characterClass)
    {
        var services = ApplicationServices.CreateDefault();

        return services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Human, characterClass));
    }
}
