using MyTurn.Domain;

namespace MyTurn.Tests.Items;

[TestFixture]
public sealed class InventoryTests
{
    [Test]
    public void Add_StacksStackableItems()
    {
        var inventory = new Inventory();
        var material = new MaterialDefinition("fiber", "Fiber");

        inventory.Add(material, 2);
        inventory.Add(material, 3);

        Assert.That(inventory.GetQuantity("fiber"), Is.EqualTo(5));
    }

    [Test]
    public void Remove_DecreasesStackAndRemovesEmptyStack()
    {
        var inventory = new Inventory();
        var potion = new ConsumableDefinition("potion", "Potion", 10);
        inventory.Add(potion, 2);

        var removedFirst = inventory.Remove("potion");
        var removedSecond = inventory.Remove("potion");

        Assert.Multiple(() =>
        {
            Assert.That(removedFirst, Is.True);
            Assert.That(removedSecond, Is.True);
            Assert.That(inventory.GetQuantity("potion"), Is.EqualTo(0));
        });
    }

    [Test]
    public void Add_CanTrackDuplicateEquipmentDefinitions()
    {
        var inventory = new Inventory();
        var armor = new ArmorDefinition("cloth-hat", "Cloth Hat", EquipmentSlot.Head, []);

        inventory.Add(armor);
        inventory.Add(armor);

        Assert.That(inventory.GetQuantity("cloth-hat"), Is.EqualTo(2));
    }

    [Test]
    public void Currency_UsesLongAmounts()
    {
        var inventory = new Inventory();
        var amount = (long)int.MaxValue + 5;

        inventory.AddCurrency(amount);
        var spent = inventory.SpendCurrency(5);

        Assert.Multiple(() =>
        {
            Assert.That(spent, Is.True);
            Assert.That(inventory.Currency, Is.EqualTo((long)int.MaxValue));
        });
    }
}
