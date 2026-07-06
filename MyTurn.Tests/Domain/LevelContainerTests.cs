using MyTurn.Domain;

namespace MyTurn.Tests.Domain;

[TestFixture]
public sealed class LevelContainerTests
{
    [Test]
    public void Constructor_ClampsCurrentLevelToBounds()
    {
        var lowLevel = new LevelContainer("Melee", -5, 0);
        var highLevel = new LevelContainer("Melee", 500, 0);

        Assert.Multiple(() =>
        {
            Assert.That(lowLevel.CurrentLevel, Is.EqualTo(1));
            Assert.That(highLevel.CurrentLevel, Is.EqualTo(100));
        });
    }

    [Test]
    public void AddExperience_LevelsUpAndCarriesRemainder()
    {
        var leveling = new LevelContainer("Melee", 1, 0);

        leveling.AddExperience(150);

        Assert.Multiple(() =>
        {
            Assert.That(leveling.CurrentLevel, Is.EqualTo(2));
            Assert.That(leveling.Experience, Is.EqualTo(50));
        });
    }

    [Test]
    public void LoseExperience_CanLevelDownWithoutGoingBelowLevelOne()
    {
        var leveling = new LevelContainer("Melee", 2, 0);

        leveling.LoseExperience(500);

        Assert.Multiple(() =>
        {
            Assert.That(leveling.CurrentLevel, Is.EqualTo(1));
            Assert.That(leveling.Experience, Is.EqualTo(0));
        });
    }

    [Test]
    public void SetLevel_ClampsAndClearsExperience()
    {
        var leveling = new LevelContainer("Melee", 1, 25);

        leveling.SetLevel(200);

        Assert.Multiple(() =>
        {
            Assert.That(leveling.CurrentLevel, Is.EqualTo(100));
            Assert.That(leveling.Experience, Is.EqualTo(0));
        });
    }
}
