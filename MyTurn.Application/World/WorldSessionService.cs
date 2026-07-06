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

    public bool HasActiveSession(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return _sessions.TryGetValue(actor.Id, out var session) && !session.IsCompleted;
    }

    public WorldSession GetOrCreate(Actor actor, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (_sessions.TryGetValue(actor.Id, out var session) && !session.IsCompleted)
        {
            return session;
        }

        return CreateNew(actor, seed);
    }

    public WorldSession CreateNew(Actor actor, int? seed = null)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var session = new WorldSession(_worldGenerator.Generate(new WorldGenerationRequest(seed)));
        _sessions[actor.Id] = session;

        return session;
    }

    public void SetActiveSession(Actor actor, WorldSession session)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(session);

        _sessions[actor.Id] = session;
    }
}
