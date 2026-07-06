using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class PartyService : IPartyService
{
    public Party CreateParty(IEnumerable<Actor> activeMembers, Inventory? sharedInventory = null)
    {
        var activeMemberList = activeMembers?.ToArray() ?? throw new ArgumentNullException(nameof(activeMembers));
        return new Party(activeMemberList, inventory: sharedInventory ?? CreateSharedInventory(activeMemberList));
    }

    public void AddRecruit(Party party, Actor actor, PartyMemberLocation location = PartyMemberLocation.Reserve)
    {
        ArgumentNullException.ThrowIfNull(party);
        party.AddRecruit(actor, location);
    }

    public void MoveToActive(Party party, Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(party);
        party.MoveToActive(actorId);
    }

    public void MoveToReserve(Party party, Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(party);
        party.MoveToReserve(actorId);
    }

    private static Inventory CreateSharedInventory(IEnumerable<Actor> members)
    {
        var inventory = new Inventory();

        foreach (var member in members)
        {
            if (member.Inventory.Currency > 0)
            {
                inventory.AddCurrency(member.Inventory.Currency);
            }

            foreach (var stack in member.Inventory.Items)
            {
                inventory.Add(stack.Item, stack.Quantity);
            }
        }

        return inventory;
    }
}
