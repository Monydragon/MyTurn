using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class WorldGenerator : IWorldGenerator
{
    public WorldMap Generate(WorldGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Size < 3 || request.Size % 2 == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Size), "World size must be an odd number greater than or equal to 3.");
        }

        var seed = request.Seed ?? Environment.TickCount;
        var random = new SeededRandomSource(seed);
        var radius = request.Size / 2;
        var min = -radius;
        var max = radius;
        var exitPosition = ChooseExitPosition(random, min, max);
        var rooms = new List<WorldRoom>();

        for (var y = min; y <= max; y++)
        {
            for (var x = min; x <= max; x++)
            {
                var position = new WorldPosition(x, y);
                var roomType = position == WorldPosition.Origin
                    ? RoomType.Start
                    : position == exitPosition
                        ? RoomType.Exit
                        : ChooseRoomType(random);

                var encounterSeed = roomType == RoomType.Enemy
                    ? DeriveRoomSeed(seed, position)
                    : (int?)null;

                rooms.Add(new WorldRoom(position, roomType, encounterSeed));
            }
        }

        return new WorldMap(seed, min, max, rooms);
    }

    private static WorldPosition ChooseExitPosition(IRandomSource random, int min, int max)
    {
        var candidates = new[]
        {
            new WorldPosition(min, min),
            new WorldPosition(min, max),
            new WorldPosition(max, min),
            new WorldPosition(max, max)
        };

        return candidates[random.NextInclusive(0, candidates.Length - 1)];
    }

    private static RoomType ChooseRoomType(IRandomSource random)
    {
        var roll = random.NextInclusive(1, 100);

        return roll switch
        {
            <= 60 => RoomType.Empty,
            <= 90 => RoomType.Enemy,
            _ => RoomType.Treasure
        };
    }

    internal static int DeriveRoomSeed(int worldSeed, WorldPosition position)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + worldSeed;
            hash = hash * 31 + position.X;
            hash = hash * 31 + position.Y;
            hash = hash * 31 + 1543;
            return hash;
        }
    }
}
