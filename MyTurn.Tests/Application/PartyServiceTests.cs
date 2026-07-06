using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.Application;

[TestFixture]
public sealed class PartyServiceTests
{
    [Test]
    public void CreateParty_RequiresOneToFourActiveMembers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new Party([]), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new Party(Enumerable.Range(1, 5).Select(index => CreateActor($"Member {index}"))),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void CreateParty_MergesStartingInventoryIntoSharedPartyInventory()
    {
        var services = ApplicationServices.CreateDefault();
        var leader = CreateActor("Avery");
        var ally = CreateActor("Blake");

        var party = services.PartyService.CreateParty([leader, ally]);

        Assert.Multiple(() =>
        {
            Assert.That(party.ActiveMembers.Select(member => member.Name), Is.EqualTo(new[] { "Avery", "Blake" }));
            Assert.That(party.Inventory.GetQuantity("small-healing-potion"), Is.EqualTo(6));
            Assert.That(party.Inventory.GetQuantity("cloth-tunic"), Is.EqualTo(2));
        });
    }

    [Test]
    public void MoveToReserve_CannotRemoveLastActiveMember()
    {
        var party = new Party([CreateActor("Avery")]);

        Assert.That(() => party.MoveToReserve(party.Leader.Id), Throws.InvalidOperationException);
    }

    [Test]
    public void MoveToActive_AndMoveToReserve_UpdateMemberLocation()
    {
        var leader = CreateActor("Avery");
        var ally = CreateActor("Blake");
        var reserve = CreateActor("Casey");
        var party = new Party([leader, ally], [reserve]);

        party.MoveToReserve(ally.Id);
        party.MoveToActive(reserve.Id);

        Assert.Multiple(() =>
        {
            Assert.That(party.ActiveMembers.Select(member => member.Name), Is.EqualTo(new[] { "Avery", "Casey" }));
            Assert.That(party.ReserveMembers.Select(member => member.Name), Is.EqualTo(new[] { "Blake" }));
            Assert.That(party.GetLocation(reserve.Id), Is.EqualTo(PartyMemberLocation.Active));
            Assert.That(party.GetLocation(ally.Id), Is.EqualTo(PartyMemberLocation.Reserve));
        });
    }

    [Test]
    public void AddSteps_IncrementsPartyAndActiveMembersOnly()
    {
        var leader = CreateActor("Avery");
        var ally = CreateActor("Blake");
        var reserve = CreateActor("Casey");
        var party = new Party([leader, ally], [reserve]);

        party.AddSteps(3);

        Assert.Multiple(() =>
        {
            Assert.That(party.Steps, Is.EqualTo(3));
            Assert.That(leader.Steps, Is.EqualTo(3));
            Assert.That(ally.Steps, Is.EqualTo(3));
            Assert.That(reserve.Steps, Is.EqualTo(0));
        });
    }

    [Test]
    public void QuickStartPartyFactory_CreatesGeneratedFullPartyWithSharedInventory()
    {
        var services = ApplicationServices.CreateDefault();

        var party = services.QuickStartPartyFactory.CreateParty(seed: 42);

        Assert.Multiple(() =>
        {
            Assert.That(party.ActiveMembers, Has.Count.EqualTo(Party.MaxActiveMembers));
            Assert.That(
                party.ActiveMembers.Select(member => member.CharacterClass),
                Is.EqualTo(new[] { CharacterClass.Warrior, CharacterClass.Archer, CharacterClass.Mage, CharacterClass.Warrior }));
            Assert.That(party.Inventory.GetQuantity("small-healing-potion"), Is.EqualTo(12));
            Assert.That(party.Inventory.GetQuantity("cloth-tunic"), Is.EqualTo(4));
        });
    }

    [Test]
    public void QuickStartPartyFactory_GeneratesStableNamesWhenSeeded()
    {
        var services = ApplicationServices.CreateDefault();

        var first = services.QuickStartPartyFactory.GenerateName(seed: 123);
        var second = services.QuickStartPartyFactory.GenerateName(seed: 123);

        Assert.That(second, Is.EqualTo(first));
    }

    private static Actor CreateActor(string name)
    {
        var services = ApplicationServices.CreateDefault();

        return services.ActorFactory.Create(new CreateActorRequest(name, 24, Gender.Other, Species.Human, CharacterClass.Warrior));
    }
}
