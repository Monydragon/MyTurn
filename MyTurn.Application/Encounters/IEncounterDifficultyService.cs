using MyTurn.Domain;

namespace MyTurn.Application;

public interface IEncounterDifficultyService
{
    int GetPartyPower(Party party);
    int GetDifficultyBudget(Party party, int worldDepth = 1);
}
