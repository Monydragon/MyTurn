namespace MyTurn.Application;

public sealed class CharacterCreationValidator : ICharacterCreationValidator
{
    public void Validate(CreateActorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Actor name is required.", nameof(request));
        }

        if (request.Age <= 17)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Age), "Actor age must be greater than 17.");
        }
    }
}
