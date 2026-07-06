using MyTurn.Domain;

namespace MyTurn.Application;

public sealed record CreateActorRequest(
    string Name,
    int Age,
    Gender Gender,
    Species Species,
    CharacterClass CharacterClass);
