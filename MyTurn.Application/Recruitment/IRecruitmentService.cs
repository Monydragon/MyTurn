using MyTurn.Domain;

namespace MyTurn.Application;

public interface IRecruitmentService
{
    void SendToInn(Party party, Actor recruit);
}
