using MyTurn.Domain;

namespace MyTurn.Tests.Domain;

[TestFixture]
public sealed class StatTests
{
    [Test]
    public void CurrentValue_IncludesModifiers()
    {
        var stat = new Stat(StatType.MeleeAttack, 10, 20);

        stat.AddModifiers(2, -1, 4);

        Assert.That(stat.CurrentValue, Is.EqualTo(15));
    }

    [Test]
    public void RemoveModifier_RemovesOneMatchingModifier()
    {
        var stat = new Stat(StatType.MeleeAttack, 10, 20, [2, 2, 5]);

        stat.RemoveModifier(2);

        Assert.Multiple(() =>
        {
            Assert.That(stat.Modifiers, Is.EquivalentTo(new[] { 2, 5 }));
            Assert.That(stat.CurrentValue, Is.EqualTo(17));
        });
    }

    [Test]
    public void ClearModifiers_RemovesAllModifiers()
    {
        var stat = new Stat(StatType.MeleeAttack, 10, 20, [2, 5]);

        stat.ClearModifiers();

        Assert.Multiple(() =>
        {
            Assert.That(stat.Modifiers, Is.Empty);
            Assert.That(stat.CurrentValue, Is.EqualTo(10));
        });
    }
}
