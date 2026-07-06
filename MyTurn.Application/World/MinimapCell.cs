using MyTurn.Domain;

namespace MyTurn.Application;

public sealed record MinimapCell(
    WorldPosition Position,
    char Symbol,
    bool IsVisible);
