using MyTurn.Presentation.Input;

namespace MyTurn.Console.Input;

public sealed class ConsoleKeyboardInputSource : IInputSource
{
    public InputSnapshot Poll()
    {
        var commands = new HashSet<GameCommand>();

        try
        {
            while (System.Console.KeyAvailable)
            {
                var key = System.Console.ReadKey(intercept: true);
                AddMappedCommand(commands, key);
            }
        }
        catch (InvalidOperationException)
        {
            return InputSnapshot.Empty;
        }

        return new InputSnapshot(commands, commands);
    }

    public void Dispose()
    {
    }

    private static void AddMappedCommand(HashSet<GameCommand> commands, ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.W:
            case ConsoleKey.UpArrow:
                commands.Add(GameCommand.MoveUp);
                break;
            case ConsoleKey.S:
            case ConsoleKey.DownArrow:
                commands.Add(GameCommand.MoveDown);
                break;
            case ConsoleKey.A:
            case ConsoleKey.LeftArrow:
                commands.Add(GameCommand.MoveLeft);
                break;
            case ConsoleKey.D:
            case ConsoleKey.RightArrow:
                commands.Add(GameCommand.MoveRight);
                break;
            case ConsoleKey.Enter:
            case ConsoleKey.Spacebar:
                commands.Add(GameCommand.Confirm);
                break;
            case ConsoleKey.Escape:
            case ConsoleKey.Backspace:
                commands.Add(GameCommand.Cancel);
                break;
            case ConsoleKey.Tab:
            case ConsoleKey.PageDown:
                commands.Add(GameCommand.Next);
                break;
            case ConsoleKey.PageUp:
                commands.Add(GameCommand.Previous);
                break;
            case ConsoleKey.M:
                commands.Add(GameCommand.Menu);
                break;
            case ConsoleKey.E:
                commands.Add(GameCommand.Primary);
                break;
            case ConsoleKey.X:
                commands.Add(GameCommand.Secondary);
                break;
        }
    }
}
