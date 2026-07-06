namespace MyTurn.Domain;

public sealed class Party
{
    public const int MinActiveMembers = 1;
    public const int MaxActiveMembers = 4;

    private readonly List<Actor> _activeMembers;
    private readonly List<Actor> _reserveMembers;

    public Guid Id { get; }
    public Inventory Inventory { get; }
    public IReadOnlyList<Actor> ActiveMembers => _activeMembers;
    public IReadOnlyList<Actor> ReserveMembers => _reserveMembers;
    public IReadOnlyList<Actor> Roster => _activeMembers.Concat(_reserveMembers).ToArray();
    public Actor Leader => _activeMembers[0];
    public long Steps { get; private set; }

    public Party(
        IEnumerable<Actor> activeMembers,
        IEnumerable<Actor>? reserveMembers = null,
        Inventory? inventory = null,
        long steps = 0,
        Guid? id = null)
    {
        _activeMembers = activeMembers?.ToList() ?? throw new ArgumentNullException(nameof(activeMembers));
        _reserveMembers = reserveMembers?.ToList() ?? [];

        ValidateActiveMemberCount(_activeMembers.Count);
        EnsureUniqueMembers(_activeMembers.Concat(_reserveMembers));

        Id = id ?? Guid.NewGuid();
        Inventory = inventory ?? new Inventory();
        Steps = Math.Max(0, steps);
    }

    public void AddRecruit(Actor actor, PartyMemberLocation location = PartyMemberLocation.Reserve)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (Roster.Any(member => member.Id == actor.Id))
        {
            throw new InvalidOperationException($"'{actor.Name}' is already in the party roster.");
        }

        if (location == PartyMemberLocation.Active)
        {
            if (_activeMembers.Count >= MaxActiveMembers)
            {
                throw new InvalidOperationException("The active party is full.");
            }

            _activeMembers.Add(actor);
            return;
        }

        _reserveMembers.Add(actor);
    }

    public void MoveToActive(Guid actorId)
    {
        if (_activeMembers.Any(member => member.Id == actorId))
        {
            return;
        }

        if (_activeMembers.Count >= MaxActiveMembers)
        {
            throw new InvalidOperationException("The active party is full.");
        }

        var actor = RemoveReserve(actorId);
        _activeMembers.Add(actor);
    }

    public void MoveToReserve(Guid actorId)
    {
        if (_activeMembers.Count <= MinActiveMembers)
        {
            throw new InvalidOperationException("At least one party member must remain active.");
        }

        var actor = RemoveActive(actorId);
        _reserveMembers.Add(actor);
    }

    public PartyMemberLocation GetLocation(Guid actorId)
    {
        if (_activeMembers.Any(member => member.Id == actorId))
        {
            return PartyMemberLocation.Active;
        }

        if (_reserveMembers.Any(member => member.Id == actorId))
        {
            return PartyMemberLocation.Reserve;
        }

        throw new KeyNotFoundException($"No party member '{actorId}' exists.");
    }

    public void AddSteps(long amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Step amount cannot be negative.");
        }

        Steps += amount;

        foreach (var member in _activeMembers)
        {
            member.AddSteps(amount);
        }
    }

    private Actor RemoveActive(Guid actorId)
    {
        var actor = _activeMembers.SingleOrDefault(member => member.Id == actorId)
            ?? throw new KeyNotFoundException($"Active party member '{actorId}' was not found.");

        _activeMembers.Remove(actor);

        return actor;
    }

    private Actor RemoveReserve(Guid actorId)
    {
        var actor = _reserveMembers.SingleOrDefault(member => member.Id == actorId)
            ?? throw new KeyNotFoundException($"Reserve party member '{actorId}' was not found.");

        _reserveMembers.Remove(actor);

        return actor;
    }

    private static void ValidateActiveMemberCount(int count)
    {
        if (count is < MinActiveMembers or > MaxActiveMembers)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Active party size must be between 1 and 4.");
        }
    }

    private static void EnsureUniqueMembers(IEnumerable<Actor> members)
    {
        var duplicate = members
            .GroupBy(member => member.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException($"Party member '{duplicate.First().Name}' appears more than once.", nameof(members));
        }
    }
}
