using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.Domain;

[TestFixture]
public sealed class ActorStepTests
{
    [Test]
    public void NewActor_StartsWithZeroSteps()
    {
        var actor = CreateActor();

        Assert.That(actor.Steps, Is.EqualTo(0));
    }

    [Test]
    public void AddSteps_TracksLongStepCounts()
    {
        var actor = CreateActor();
        var longStepCount = (long)int.MaxValue + 100;

        actor.AddSteps(longStepCount);

        Assert.That(actor.Steps, Is.EqualTo(longStepCount));
    }

    [Test]
    public void AddSteps_RejectsNegativeValues()
    {
        var actor = CreateActor();

        Assert.That(() => actor.AddSteps(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void ResetSteps_SetsStepCountToZero()
    {
        var actor = CreateActor();
        actor.AddSteps(50);

        actor.ResetSteps();

        Assert.That(actor.Steps, Is.EqualTo(0));
    }

    private static Actor CreateActor()
    {
        var services = ApplicationServices.CreateDefault();

        return services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Human, CharacterClass.Warrior));
    }
}
