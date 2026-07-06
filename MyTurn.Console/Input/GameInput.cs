namespace MyTurn.Console.Input;

public static class GameInput
{
    public static CompositeInputReader CreateDefault()
    {
        var mappingPath = Path.Combine(AppContext.BaseDirectory, "Input", "gamecontrollerdb.txt");

        return new CompositeInputReader(
        [
            new WindowsXInputSource(),
            new SdlGameControllerInputSource(mappingPath),
            new ConsoleKeyboardInputSource()
        ]);
    }
}
