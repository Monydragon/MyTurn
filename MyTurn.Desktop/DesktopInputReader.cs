using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MyTurn.Presentation.Input;

namespace MyTurn.Desktop;

internal sealed class DesktopInputReader
{
    private const float StickDeadzone = 0.35f;
    private readonly HashSet<GameCommand> _previousHeldCommands = [];

    public InputSnapshot Poll()
    {
        var keyboard = Keyboard.GetState();
        var gamePad = GamePad.GetState(PlayerIndex.One);
        var held = new HashSet<GameCommand>();

        AddKeyboardCommands(keyboard, held);
        AddGamePadCommands(gamePad, held);

        var pressed = held.Except(_previousHeldCommands).ToArray();
        _previousHeldCommands.Clear();
        _previousHeldCommands.UnionWith(held);

        return new InputSnapshot(
            pressed,
            held,
            gamePad.IsConnected ? "GamePad 1" : null);
    }

    private static void AddKeyboardCommands(KeyboardState keyboard, HashSet<GameCommand> commands)
    {
        AddIf(commands, keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up), GameCommand.MoveUp);
        AddIf(commands, keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down), GameCommand.MoveDown);
        AddIf(commands, keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left), GameCommand.MoveLeft);
        AddIf(commands, keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right), GameCommand.MoveRight);
        AddIf(commands, keyboard.IsKeyDown(Keys.Enter) || keyboard.IsKeyDown(Keys.Space), GameCommand.Confirm);
        AddIf(commands, keyboard.IsKeyDown(Keys.Escape) || keyboard.IsKeyDown(Keys.Back), GameCommand.Cancel);
        AddIf(commands, keyboard.IsKeyDown(Keys.M), GameCommand.Menu);
        AddIf(commands, keyboard.IsKeyDown(Keys.PageUp), GameCommand.Previous);
        AddIf(commands, keyboard.IsKeyDown(Keys.Tab) || keyboard.IsKeyDown(Keys.PageDown), GameCommand.Next);
        AddIf(commands, keyboard.IsKeyDown(Keys.E), GameCommand.Primary);
        AddIf(commands, keyboard.IsKeyDown(Keys.X), GameCommand.Secondary);
    }

    private static void AddGamePadCommands(GamePadState gamePad, HashSet<GameCommand> commands)
    {
        if (!gamePad.IsConnected)
        {
            return;
        }

        AddIf(commands, gamePad.DPad.Up == ButtonState.Pressed || gamePad.ThumbSticks.Left.Y > StickDeadzone, GameCommand.MoveUp);
        AddIf(commands, gamePad.DPad.Down == ButtonState.Pressed || gamePad.ThumbSticks.Left.Y < -StickDeadzone, GameCommand.MoveDown);
        AddIf(commands, gamePad.DPad.Left == ButtonState.Pressed || gamePad.ThumbSticks.Left.X < -StickDeadzone, GameCommand.MoveLeft);
        AddIf(commands, gamePad.DPad.Right == ButtonState.Pressed || gamePad.ThumbSticks.Left.X > StickDeadzone, GameCommand.MoveRight);
        AddIf(commands, gamePad.Buttons.A == ButtonState.Pressed, GameCommand.Confirm);
        AddIf(commands, gamePad.Buttons.B == ButtonState.Pressed || gamePad.Buttons.Back == ButtonState.Pressed, GameCommand.Cancel);
        AddIf(commands, gamePad.Buttons.Start == ButtonState.Pressed, GameCommand.Menu);
        AddIf(commands, gamePad.Buttons.LeftShoulder == ButtonState.Pressed, GameCommand.Previous);
        AddIf(commands, gamePad.Buttons.RightShoulder == ButtonState.Pressed, GameCommand.Next);
        AddIf(commands, gamePad.Buttons.Y == ButtonState.Pressed, GameCommand.Primary);
        AddIf(commands, gamePad.Buttons.X == ButtonState.Pressed, GameCommand.Secondary);
    }

    private static void AddIf(HashSet<GameCommand> commands, bool condition, GameCommand command)
    {
        if (condition)
        {
            commands.Add(command);
        }
    }
}
