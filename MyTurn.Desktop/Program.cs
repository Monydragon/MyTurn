namespace MyTurn.Desktop;

internal static class Program
{
    private static void Main(string[] args)
    {
        using var game = new MyTurnGame(DesktopRunOptions.Parse(args));
        game.Run();
    }
}
