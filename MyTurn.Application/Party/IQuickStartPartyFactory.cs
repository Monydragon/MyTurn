using MyTurn.Domain;

namespace MyTurn.Application;

public interface IQuickStartPartyFactory
{
    Party CreateParty(int activeMemberCount = Party.MaxActiveMembers, int? seed = null);
    CreateActorRequest CreateActorRequest(int memberIndex, int? seed = null);
    string GenerateName(int? seed = null);
}
