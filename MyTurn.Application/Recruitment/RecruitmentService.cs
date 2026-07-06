using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class RecruitmentService : IRecruitmentService
{
    public void SendToInn(Party party, Actor recruit)
    {
        ArgumentNullException.ThrowIfNull(party);
        party.AddRecruit(recruit, PartyMemberLocation.Reserve);
    }
}
