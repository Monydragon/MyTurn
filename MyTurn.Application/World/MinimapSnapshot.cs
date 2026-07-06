using MyTurn.Domain;

namespace MyTurn.Application;

public sealed record MinimapSnapshot(
    IReadOnlyList<MinimapCell> Cells,
    int MinCoordinate,
    int MaxCoordinate)
{
    public MinimapCell GetCell(WorldPosition position)
    {
        return Cells.First(cell => cell.Position == position);
    }
}
