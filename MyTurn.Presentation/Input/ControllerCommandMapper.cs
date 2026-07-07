namespace MyTurn.Presentation.Input;

public sealed class ControllerCommandMapper
{
    public const short DefaultDeadzone = 16_000;

    private readonly short _deadzone;

    public ControllerCommandMapper(short deadzone = DefaultDeadzone)
    {
        _deadzone = deadzone;
    }

    public IReadOnlySet<GameCommand> Map(ControllerInputState state)
    {
        var commands = new HashSet<GameCommand>();

        AddIf(commands, state.A, GameCommand.Confirm);
        AddIf(commands, state.B || state.Back, GameCommand.Cancel);
        AddIf(commands, state.X, GameCommand.Secondary);
        AddIf(commands, state.Y, GameCommand.Primary);
        AddIf(commands, state.Start, GameCommand.Menu);
        AddIf(commands, state.LeftShoulder, GameCommand.Previous);
        AddIf(commands, state.RightShoulder, GameCommand.Next);
        AddIf(commands, state.DpadUp || state.LeftY < -_deadzone, GameCommand.MoveUp);
        AddIf(commands, state.DpadDown || state.LeftY > _deadzone, GameCommand.MoveDown);
        AddIf(commands, state.DpadLeft || state.LeftX < -_deadzone, GameCommand.MoveLeft);
        AddIf(commands, state.DpadRight || state.LeftX > _deadzone, GameCommand.MoveRight);

        return commands;
    }

    private static void AddIf(HashSet<GameCommand> commands, bool condition, GameCommand command)
    {
        if (condition)
        {
            commands.Add(command);
        }
    }
}

public sealed record ControllerInputState(
    bool A = false,
    bool B = false,
    bool X = false,
    bool Y = false,
    bool Back = false,
    bool Start = false,
    bool LeftShoulder = false,
    bool RightShoulder = false,
    bool DpadUp = false,
    bool DpadDown = false,
    bool DpadLeft = false,
    bool DpadRight = false,
    short LeftX = 0,
    short LeftY = 0);
