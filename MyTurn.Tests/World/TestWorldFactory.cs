using MyTurn.Application;
using MyTurn.Domain;

namespace MyTurn.Tests.World;

internal static class TestWorldFactory
{
    public static Actor CreateActor()
    {
        var services = ApplicationServices.CreateDefault();

        return services.ActorFactory.Create(new CreateActorRequest("Avery", 24, Gender.Other, Species.Human, CharacterClass.Warrior));
    }

    public static WorldMap CreateMap(IReadOnlyDictionary<WorldPosition, RoomType> rooms, int min, int max)
    {
        return new WorldMap(42, min, max, rooms.Select(room => new WorldRoom(room.Key, room.Value)));
    }

    public static IExplorationService CreateExplorationService()
    {
        var services = ApplicationServices.CreateDefault();

        return services.ExplorationService;
    }
}
