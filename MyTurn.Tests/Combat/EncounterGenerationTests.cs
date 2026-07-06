using MyTurn.Application;

namespace MyTurn.Tests.Combat;

[TestFixture]
public sealed class EncounterGenerationTests
{
    [Test]
    public void Generate_WithSameSeed_ReturnsSameEnemyGroup()
    {
        var services = ApplicationServices.CreateDefault();

        var first = services.EncounterGenerator.Generate(seed: 1234);
        var second = services.EncounterGenerator.Generate(seed: 1234);

        Assert.That(second.Enemies.Select(enemy => enemy.Id), Is.EqualTo(first.Enemies.Select(enemy => enemy.Id)));
    }
}
