namespace MyTurn.Console.Input;

public sealed class MovementRepeatController
{
    public static readonly TimeSpan DefaultInitialDelay = TimeSpan.FromMilliseconds(140);
    public static readonly TimeSpan DefaultRepeatDelay = TimeSpan.FromMilliseconds(90);

    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _repeatDelay;
    private GameCommand? _heldDirection;
    private DateTimeOffset _nextRepeatAt;

    public MovementRepeatController(TimeSpan? initialDelay = null, TimeSpan? repeatDelay = null)
    {
        _initialDelay = initialDelay ?? DefaultInitialDelay;
        _repeatDelay = repeatDelay ?? DefaultRepeatDelay;
    }

    public bool TryConsume(InputSnapshot snapshot, DateTimeOffset now, out GameCommand command)
    {
        var direction = GetDirection(snapshot);

        if (direction is null)
        {
            _heldDirection = null;
            _nextRepeatAt = default;
            command = default;
            return false;
        }

        if (_heldDirection != direction || snapshot.IsPressed(direction.Value))
        {
            _heldDirection = direction;
            _nextRepeatAt = now + _initialDelay;
            command = direction.Value;
            return true;
        }

        if (now >= _nextRepeatAt)
        {
            _nextRepeatAt = now + _repeatDelay;
            command = direction.Value;
            return true;
        }

        command = default;
        return false;
    }

    private static GameCommand? GetDirection(InputSnapshot snapshot)
    {
        foreach (var command in DirectionCommands)
        {
            if (snapshot.IsPressed(command))
            {
                return command;
            }
        }

        foreach (var command in DirectionCommands)
        {
            if (snapshot.IsHeld(command))
            {
                return command;
            }
        }

        return null;
    }

    private static readonly GameCommand[] DirectionCommands =
    [
        GameCommand.MoveUp,
        GameCommand.MoveDown,
        GameCommand.MoveLeft,
        GameCommand.MoveRight
    ];
}
