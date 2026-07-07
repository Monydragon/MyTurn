namespace MyTurn.Application;

public interface IWorldLayoutGenerator : IWorldGenerator
{
    WorldLayout GenerateLayout(WorldGenerationRequest request);
}
