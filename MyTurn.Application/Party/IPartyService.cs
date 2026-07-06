using MyTurn.Domain;

namespace MyTurn.Application;

public interface IPartyService
{
    Party CreateParty(IEnumerable<Actor> activeMembers, Inventory? sharedInventory = null);
    void AddRecruit(Party party, Actor actor, PartyMemberLocation location = PartyMemberLocation.Reserve);
    void MoveToActive(Party party, Guid actorId);
    void MoveToReserve(Party party, Guid actorId);
}
