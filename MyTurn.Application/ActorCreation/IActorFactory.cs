using MyTurn.Domain;

namespace MyTurn.Application;

public interface IActorFactory
{
    Actor Create(CreateActorRequest request);
}
