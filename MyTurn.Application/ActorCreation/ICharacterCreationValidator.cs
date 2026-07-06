namespace MyTurn.Application;

public interface ICharacterCreationValidator
{
    void Validate(CreateActorRequest request);
}
