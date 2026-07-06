using MyTurn.Domain;

namespace MyTurn.Application;

public interface IEncounterGenerator
{
    Encounter Generate(int difficulty = 1, int? seed = null);
}
