using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyTurn.Domain;
using MyTurn.Presentation;

namespace MyTurn.Desktop;

internal sealed class DesktopRenderer
{
    private readonly DesktopLayout _layout;
    private readonly JrpgWindowRenderer _windows;
    private readonly TileMapRenderer _tileMapRenderer;
    private readonly BattleSceneRenderer _battleRenderer;

    public DesktopRenderer(SpriteBatch spriteBatch, Texture2D pixel, DesktopAssetCatalog assets, int width, int height)
    {
        var text = new BitmapTextRenderer(spriteBatch, pixel);
        _layout = new DesktopLayout(width, height);
        _windows = new JrpgWindowRenderer(spriteBatch, pixel, text);
        _tileMapRenderer = new TileMapRenderer(spriteBatch, _windows, assets);
        _battleRenderer = new BattleSceneRenderer(_windows);
    }

    public void Draw(GameViewModel view)
    {
        DrawBackdrop();

        switch (view)
        {
            case MainMenuViewModel mainMenu:
                DrawMainMenu(mainMenu);
                break;
            case LoadGameViewModel loadGame:
                DrawLoadGame(loadGame);
                break;
            case HubViewModel hub:
                DrawHub(hub);
                break;
            case WorldViewModel world:
                DrawWorld(world);
                break;
            case CombatViewModel combat:
                DrawCombat(combat);
                break;
            case PartyViewModel party:
                DrawParty(party);
                break;
            case InventoryViewModel inventory:
                DrawInventory(inventory);
                break;
            case MessageViewModel message:
                DrawMessage(message);
                break;
        }
    }

    private void DrawMainMenu(MainMenuViewModel view)
    {
        DrawTitle("MY TURN");
        _windows.DrawWindow(_layout.MainMenuWindow, "START");
        _windows.DrawMenu(view.Options, DesktopLayout.Inset(_layout.MainMenuWindow, 54), 4);
        DrawFooter("MOVE: DPAD / LEFT STICK / WASD     SELECT: A / ENTER");
    }

    private void DrawLoadGame(LoadGameViewModel view)
    {
        DrawTitle("LOAD GAME");
        _windows.DrawWindow(_layout.LoadGameWindow, "SAVES");
        _windows.DrawMenu(view.Options, DesktopLayout.Inset(_layout.LoadGameWindow, 48), 3);
        DrawFooter("LOAD: A / ENTER     BACK: B / ESC");
    }

    private void DrawHub(HubViewModel view)
    {
        DrawTitle("CAMP");
        DrawPartySummary(view.Party, _layout.Hub.PartyPanel);
        _windows.DrawWindow(_layout.Hub.CommandPanel, "ACTIONS");
        _windows.DrawMenu(view.Options, DesktopLayout.Inset(_layout.Hub.CommandPanel, 54), 3);
        DrawFooter("MOVE: DPAD / LEFT STICK / WASD     SELECT: A / ENTER");
    }

    private void DrawWorld(WorldViewModel view)
    {
        var world = _layout.World;

        _windows.DrawWindow(world.Field, "FIELD");
        var fieldContent = DesktopLayout.Inset(world.Field, 14);

        if (!_tileMapRenderer.Draw(view, fieldContent))
        {
            DrawMinimap(view, fieldContent);
        }

        _windows.DrawWindow(world.StatusPanel, "STATUS");
        DrawWorldStatus(view, DesktopLayout.Inset(world.StatusPanel, 28));

        _windows.DrawWindow(world.DialoguePanel);
        var message = GetWorldDialogue(view);
        _windows.DrawWrappedText(message, DesktopLayout.Inset(world.DialoguePanel, 18), view.HasPendingEncounter ? Color.OrangeRed : Color.LightGray, 2);
    }

    private void DrawCombat(CombatViewModel view)
    {
        _battleRenderer.Draw(view, _layout.Combat);
        DrawFooter("SELECT: A / ENTER     BACK: B / ESC     TARGET: LB / RB");
    }

    private void DrawParty(PartyViewModel view)
    {
        DrawTitle(view.Title);
        _windows.DrawWindow(_layout.ListWindow, "ROSTER");
        var content = DesktopLayout.Inset(_layout.ListWindow, 42);
        var y = content.Y + 8;

        foreach (var member in view.Members.Take(8))
        {
            _windows.DrawText($"{member.Location}  {member.Name}  {member.ClassName}", content.X, y, Color.White, 3, content.Width);
            _windows.DrawText($"HP {member.CurrentHealth}/{member.MaxHealth}  {member.WeaponName}", content.X + 18, y + 32, Color.LightGray, 2, content.Width - 18);
            y += 68;
        }

        DrawFooter("CONTINUE: A / ENTER     BACK: B / ESC");
    }

    private void DrawInventory(InventoryViewModel view)
    {
        DrawTitle("INVENTORY");
        _windows.DrawWindow(_layout.ListWindow, "PARTY BAG");
        var content = DesktopLayout.Inset(_layout.ListWindow, 42);
        _windows.DrawText($"CURRENCY: {view.Currency}", content.X, content.Y + 4, Color.Gold, 3, content.Width);
        var y = content.Y + 64;

        foreach (var item in view.Items.Take(11))
        {
            _windows.DrawText($"{item.Name}  X{item.Quantity}  {item.Kind}", content.X, y, Color.White, 3, content.Width);
            y += 38;
        }

        DrawFooter("CONTINUE: A / ENTER     BACK: B / ESC");
    }

    private void DrawMessage(MessageViewModel view)
    {
        DrawTitle(view.Title);
        _windows.DrawWindow(_layout.MessageWindow, view.Title);
        _windows.DrawWrappedText(view.Message, DesktopLayout.Inset(_layout.MessageWindow, 46), Color.Gold, 3);
        DrawFooter("CONTINUE: A / ENTER     BACK: B / ESC");
    }

    private void DrawWorldStatus(WorldViewModel view, Rectangle bounds)
    {
        _windows.DrawText($"LEADER: {view.Party.LeaderName}", bounds.X, bounds.Y + 8, Color.Gold, 2, bounds.Width);
        _windows.DrawText($"ROOM: {view.RoomType}", bounds.X, bounds.Y + 44, RoomColor(view.RoomType), 3, bounds.Width);
        _windows.DrawText($"STATE: {view.RoomStatus}", bounds.X, bounds.Y + 84, Color.LightGray, 2, bounds.Width);
        _windows.DrawText($"POS: {view.CurrentPosition.X}, {view.CurrentPosition.Y}", bounds.X, bounds.Y + 116, Color.White, 2, bounds.Width);
        _windows.DrawText(view.IsMoving ? $"MOVING {view.FacingDirection}" : $"FACING {view.FacingDirection}", bounds.X, bounds.Y + 148, Color.White, 2, bounds.Width);
        _windows.DrawText($"STEPS: {view.Party.Steps}", bounds.X, bounds.Y + 180, Color.Gold, 2, bounds.Width);
        _windows.DrawText($"ACTIVE: {view.Party.ActiveCount}/{view.Party.MaxActiveCount}", bounds.X, bounds.Y + 212, Color.White, 2, bounds.Width);

        var y = bounds.Y + 262;
        foreach (var member in view.Party.ActiveMembers.Take(4))
        {
            _windows.DrawText(member.Name, bounds.X, y, Color.White, 2, 170);
            _windows.DrawText($"{member.CurrentHealth}/{member.MaxHealth}", bounds.Right - 92, y, Color.LightGray, 2);
            _windows.DrawHpBar(new Rectangle(bounds.X, y + 24, bounds.Width, 10), member.CurrentHealth, member.MaxHealth);
            y += 48;
        }
    }

    private void DrawPartySummary(PartySummaryViewModel party, Rectangle bounds)
    {
        _windows.DrawWindow(bounds, "PARTY");
        var content = DesktopLayout.Inset(bounds, 36);

        _windows.DrawText($"LEADER: {party.LeaderName}", content.X, content.Y + 10, Color.Gold, 3, content.Width);
        _windows.DrawText($"ACTIVE: {party.ActiveCount}/{party.MaxActiveCount}", content.X, content.Y + 56, Color.White, 3, content.Width);
        _windows.DrawText($"STEPS: {party.Steps}", content.X, content.Y + 102, Color.LightGray, 3, content.Width);
        _windows.DrawText($"CURRENCY: {party.Currency}", content.X, content.Y + 148, Color.Gold, 3, content.Width);

        var y = content.Y + 218;
        foreach (var member in party.ActiveMembers.Take(4))
        {
            _windows.DrawText(member.Name, content.X, y, Color.White, 2, 170);
            _windows.DrawText($"{member.CurrentHealth}/{member.MaxHealth}", content.Right - 98, y, Color.LightGray, 2);
            _windows.DrawHpBar(new Rectangle(content.X, y + 24, content.Width, 10), member.CurrentHealth, member.MaxHealth);
            y += 50;
        }
    }

    private void DrawMinimap(WorldViewModel view, Rectangle bounds)
    {
        var count = view.MaxCoordinate - view.MinCoordinate + 1;
        var cellSize = Math.Max(18, Math.Min(bounds.Width, bounds.Height) / Math.Max(1, count));
        var mapWidth = cellSize * count;
        var mapHeight = cellSize * count;
        var startX = bounds.X + ((bounds.Width - mapWidth) / 2);
        var startY = bounds.Y + ((bounds.Height - mapHeight) / 2);

        foreach (var cell in view.Cells.Where(cell => cell.IsVisible))
        {
            var x = startX + ((cell.Position.X - view.MinCoordinate) * cellSize);
            var y = startY + ((view.MaxCoordinate - cell.Position.Y) * cellSize);
            var rect = new Rectangle(x, y, cellSize - 3, cellSize - 3);
            _windows.Fill(rect, CellColor(cell.Symbol));
            _windows.DrawText(cell.Symbol.ToString(), x + (cellSize / 3), y + (cellSize / 4), Color.White, Math.Max(2, cellSize / 14));
        }
    }

    private void DrawTitle(string title)
    {
        _windows.DrawText(title, _layout.TitleBar.X, _layout.TitleBar.Y, Color.Gold, 5, _layout.TitleBar.Width);
        _windows.Fill(new Rectangle(_layout.TitleBar.X, _layout.TitleBar.Bottom + 8, _layout.TitleBar.Width, 3), new Color(82, 94, 118));
    }

    private void DrawFooter(string text)
    {
        _windows.DrawWindow(new Rectangle(_layout.Footer.X, _layout.Footer.Y - 10, _layout.Footer.Width, _layout.Footer.Height + 20));
        _windows.DrawText(text, _layout.Footer.X + 18, _layout.Footer.Y, Color.LightGray, 2, _layout.Footer.Width - 36);
    }

    private void DrawBackdrop()
    {
        _windows.Fill(new Rectangle(0, 0, _layout.Width, _layout.Height), new Color(8, 12, 20));
        _windows.Fill(new Rectangle(0, 0, _layout.Width, 210), new Color(12, 20, 34));
        _windows.Fill(new Rectangle(0, _layout.Height - 180, _layout.Width, 180), new Color(10, 15, 24));
    }

    private static string GetWorldDialogue(WorldViewModel view)
    {
        if (view.InteractionPrompt is not null)
        {
            return view.InteractionPrompt.RequiresConfirm
                ? $"{view.InteractionPrompt.Title}: {view.InteractionPrompt.Message} Confirm to interact."
                : $"{view.InteractionPrompt.Title}: {view.InteractionPrompt.Message}";
        }

        if (!string.IsNullOrWhiteSpace(view.LatestEvent))
        {
            return view.LatestEvent;
        }

        return view.HasPendingEncounter
            ? "Enemies are here. Confirm to start battle, or cancel to return to camp."
            : "Move through the dungeon. Events resolve after the party reaches the next tile.";
    }

    private static Color RoomColor(string roomType)
    {
        return roomType.Equals("Enemy", StringComparison.OrdinalIgnoreCase) ? Color.OrangeRed :
            roomType.Equals("Treasure", StringComparison.OrdinalIgnoreCase) ? Color.Gold :
            roomType.Equals("Exit", StringComparison.OrdinalIgnoreCase) ? Color.MediumPurple :
            Color.White;
    }

    private static Color CellColor(char symbol)
    {
        return symbol switch
        {
            '@' => Color.ForestGreen,
            'S' => Color.RoyalBlue,
            'E' => Color.MediumPurple,
            '!' => Color.Firebrick,
            '$' => Color.Goldenrod,
            '?' => new Color(45, 50, 62),
            _ => new Color(70, 78, 88)
        };
    }
}
