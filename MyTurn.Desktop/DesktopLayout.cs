using Microsoft.Xna.Framework;
using MyTurn.Presentation;

namespace MyTurn.Desktop;

public sealed class DesktopLayout
{
    public const int VirtualWidth = 1280;
    public const int VirtualHeight = 720;
    public const int SafeMargin = 28;

    public DesktopLayout(int width = VirtualWidth, int height = VirtualHeight)
    {
        Width = width;
        Height = height;
        SafeBounds = new Rectangle(SafeMargin, SafeMargin, width - (SafeMargin * 2), height - (SafeMargin * 2));
    }

    public int Width { get; }
    public int Height { get; }
    public Rectangle SafeBounds { get; }

    public Rectangle TitleBar => new(SafeMargin, 26, Width - (SafeMargin * 2), 42);
    public Rectangle Footer => new(SafeMargin, Height - 54, Width - (SafeMargin * 2), 28);

    public Rectangle MainMenuWindow => new(402, 170, 476, 348);
    public Rectangle LoadGameWindow => new(170, 112, 940, 514);
    public Rectangle MessageWindow => new(250, 242, 780, 220);

    public HubLayout Hub => new(
        new Rectangle(64, 106, 496, 518),
        new Rectangle(604, 106, 612, 518));

    public WorldLayout World => new(
        new Rectangle(32, 78, 842, 526),
        new Rectangle(898, 78, 350, 526),
        new Rectangle(32, 622, 1216, 64));

    public CombatLayout Combat => new(
        new Rectangle(32, 76, 1216, 338),
        new Rectangle(32, 432, 568, 206),
        new Rectangle(620, 432, 260, 206),
        new Rectangle(900, 432, 348, 206));

    public Rectangle ListWindow => new(84, 108, 1112, 514);

    public IEnumerable<Rectangle> GetScreenRectangles(ScreenKind screenKind)
    {
        return screenKind switch
        {
            ScreenKind.MainMenu => [TitleBar, MainMenuWindow, Footer],
            ScreenKind.LoadGame => [TitleBar, LoadGameWindow, Footer],
            ScreenKind.Hub => [TitleBar, Hub.PartyPanel, Hub.CommandPanel, Footer],
            ScreenKind.World => [World.Field, World.StatusPanel, World.DialoguePanel],
            ScreenKind.Combat => [Combat.Battlefield, Combat.LogPanel, Combat.CommandPanel, Combat.PartyPanel, Footer],
            ScreenKind.Party or ScreenKind.Inventory => [TitleBar, ListWindow, Footer],
            ScreenKind.Message => [TitleBar, MessageWindow, Footer],
            _ => [SafeBounds]
        };
    }

    public bool Contains(Rectangle rectangle)
    {
        return rectangle.X >= 0 &&
            rectangle.Y >= 0 &&
            rectangle.Right <= Width &&
            rectangle.Bottom <= Height;
    }

    public static Rectangle Inset(Rectangle rectangle, int padding)
    {
        var inset = padding * 2;
        return new Rectangle(
            rectangle.X + padding,
            rectangle.Y + padding,
            Math.Max(0, rectangle.Width - inset),
            Math.Max(0, rectangle.Height - inset));
    }
}

public sealed record HubLayout(Rectangle PartyPanel, Rectangle CommandPanel);

public sealed record WorldLayout(Rectangle Field, Rectangle StatusPanel, Rectangle DialoguePanel);

public sealed record CombatLayout(
    Rectangle Battlefield,
    Rectangle LogPanel,
    Rectangle CommandPanel,
    Rectangle PartyPanel);
