using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class MinimapService : IMinimapService
{
    public MinimapSnapshot CreateSnapshot(WorldSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var cells = new List<MinimapCell>();

        for (var y = session.Map.MaxCoordinate; y >= session.Map.MinCoordinate; y--)
        {
            for (var x = session.Map.MinCoordinate; x <= session.Map.MaxCoordinate; x++)
            {
                var position = new WorldPosition(x, y);
                var symbol = GetSymbol(session, position);
                cells.Add(new MinimapCell(position, symbol, symbol != ' '));
            }
        }

        return new MinimapSnapshot(cells, session.Map.MinCoordinate, session.Map.MaxCoordinate);
    }

    private static char GetSymbol(WorldSession session, WorldPosition position)
    {
        if (!session.Map.Contains(position))
        {
            return ' ';
        }

        if (position == session.CurrentPosition)
        {
            return '@';
        }

        var room = session.Map.GetRoom(position);

        if (room.IsVisited)
        {
            return room.RoomType switch
            {
                RoomType.Start => 'S',
                RoomType.Exit => 'E',
                RoomType.Enemy when !room.IsCleared => '!',
                RoomType.Treasure when !room.IsLooted => '$',
                _ => '.'
            };
        }

        return IsAdjacentToVisitedRoom(session, position) ? '?' : ' ';
    }

    private static bool IsAdjacentToVisitedRoom(WorldSession session, WorldPosition position)
    {
        return Enum.GetValues<Direction>()
            .Select(position.Move)
            .Where(session.Map.Contains)
            .Select(session.Map.GetRoom)
            .Any(room => room.IsVisited);
    }
}
