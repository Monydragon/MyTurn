using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class WorldSessionService : IWorldSessionService
{
    private readonly IWorldGenerator _worldGenerator;
    private readonly Dictionary<Guid, WorldSession> _sessions = [];

    public WorldSessionService(IWorldGenerator worldGenerator)
    {
        _worldGenerator = worldGenerator;
    }

    public bool HasActiveSession(Party party)
    {
        ArgumentNullException.ThrowIfNull(party);

        return _sessions.TryGetValue(party.Id, out var session) && !session.IsCompleted;
    }

    public WorldSession GetOrCreate(Party party, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(party);

        if (_sessions.TryGetValue(party.Id, out var session) && !session.IsCompleted)
        {
            return session;
        }

        return CreateNew(party, seed);
    }

    public WorldSession CreateNew(Party party, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(party);

        var request = new WorldGenerationRequest(seed);
        var session = _worldGenerator is IWorldLayoutGenerator layoutGenerator
            ? CreateFromLayout(layoutGenerator.GenerateLayout(request))
            : new WorldSession(_worldGenerator.Generate(request));
        _sessions[party.Id] = session;

        return session;
    }

    public void SetActiveSession(Party party, WorldSession session)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(session);

        _sessions[party.Id] = session;
    }

    private static WorldSession CreateFromLayout(WorldLayout layout)
    {
        return new WorldSession(
            layout.Map,
            layout.Map.StartPosition,
            isCompleted: false,
            layoutId: layout.LayoutId,
            profileId: layout.ProfileId,
            layoutSource: layout.LayoutSource,
            objects: layout.Objects);
    }
}
