namespace MyTurn.Presentation.Input;

public sealed class InputSnapshot
{
    private readonly HashSet<GameCommand> _pressedCommands;
    private readonly HashSet<GameCommand> _heldCommands;

    public InputSnapshot(
        IEnumerable<GameCommand>? pressedCommands = null,
        IEnumerable<GameCommand>? heldCommands = null,
        string? controllerName = null)
    {
        _pressedCommands = new HashSet<GameCommand>(pressedCommands ?? []);
        _heldCommands = new HashSet<GameCommand>(heldCommands ?? []);
        ControllerName = controllerName;
    }

    public static InputSnapshot Empty { get; } = new();

    public IReadOnlySet<GameCommand> PressedCommands => _pressedCommands;
    public IReadOnlySet<GameCommand> HeldCommands => _heldCommands;
    public string? ControllerName { get; }
    public bool HasController => !string.IsNullOrWhiteSpace(ControllerName);

    public bool IsPressed(GameCommand command)
    {
        return _pressedCommands.Contains(command);
    }

    public bool IsHeld(GameCommand command)
    {
        return _heldCommands.Contains(command);
    }

    public static InputSnapshot Merge(IEnumerable<InputSnapshot> snapshots)
    {
        var pressed = new HashSet<GameCommand>();
        var held = new HashSet<GameCommand>();
        string? controllerName = null;

        foreach (var snapshot in snapshots)
        {
            pressed.UnionWith(snapshot.PressedCommands);
            held.UnionWith(snapshot.HeldCommands);
            controllerName ??= snapshot.ControllerName;
        }

        return new InputSnapshot(pressed, held, controllerName);
    }
}
