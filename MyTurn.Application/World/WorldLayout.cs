using MyTurn.Domain;

namespace MyTurn.Application;

public sealed record WorldLayout(
    WorldMap Map,
    string LayoutId,
    string? ProfileId,
    string? LayoutSource,
    IReadOnlyList<WorldObject> Objects);
