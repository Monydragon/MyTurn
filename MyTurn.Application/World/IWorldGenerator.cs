using MyTurn.Domain;

namespace MyTurn.Application;

public interface IWorldGenerator
{
    WorldMap Generate(WorldGenerationRequest request);
}
