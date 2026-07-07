using Microsoft.Xna.Framework;
using MyTurn.Domain;
using MyTurn.Presentation;

namespace MyTurn.Desktop;

internal sealed class BattleSceneRenderer
{
    private readonly JrpgWindowRenderer _windows;

    public BattleSceneRenderer(JrpgWindowRenderer windows)
    {
        _windows = windows;
    }

    public void Draw(CombatViewModel view, CombatLayout layout)
    {
        DrawBattlefield(view, layout.Battlefield);
        DrawLog(view, layout.LogPanel);
        DrawCommandWindow(view, layout.CommandPanel);
        DrawPartyStatus(view, layout.PartyPanel);

        if (!string.IsNullOrWhiteSpace(view.OutcomeText))
        {
            var outcome = new Rectangle(326, 230, 628, 178);
            _windows.DrawWindow(outcome, view.OutcomeTitle ?? "OUTCOME");
            _windows.DrawWrappedText(view.OutcomeText, DesktopLayout.Inset(outcome, 34), Color.Gold, 3);
        }
    }

    private void DrawBattlefield(CombatViewModel view, Rectangle bounds)
    {
        _windows.Fill(bounds, new Color(12, 24, 34));
        _windows.Fill(new Rectangle(bounds.X, bounds.Bottom - 76, bounds.Width, 76), new Color(32, 52, 44));
        _windows.Fill(new Rectangle(bounds.X, bounds.Bottom - 90, bounds.Width, 18), new Color(64, 74, 58));
        _windows.Stroke(bounds, new Color(62, 88, 126));

        DrawEnemyLane(view.Enemies, new Rectangle(bounds.X + 62, bounds.Y + 42, 426, bounds.Height - 92));
        DrawPartyLane(view.Party, new Rectangle(bounds.Right - 482, bounds.Y + 34, 420, bounds.Height - 84));

        var turnText = view.ActiveCombatantName is null
            ? "RESOLVING..."
            : $"TURN: {view.ActiveCombatantName}";
        _windows.DrawText(turnText, bounds.X + 24, bounds.Y + 18, Color.Gold, 2, bounds.Width - 48);
    }

    private void DrawEnemyLane(IReadOnlyList<CombatantViewModel> enemies, Rectangle lane)
    {
        var spacing = Math.Max(74, lane.Height / Math.Max(1, enemies.Count));
        var y = lane.Y + 24;

        foreach (var enemy in enemies.Take(4))
        {
            var sprite = new Rectangle(lane.X + 54, y, 88, 58);
            var color = enemy.IsAlive ? new Color(168, 58, 64) : new Color(70, 70, 74);

            _windows.Fill(new Rectangle(sprite.X + 8, sprite.Bottom + 6, sprite.Width - 16, 8), new Color(0, 0, 0, 110));
            _windows.Fill(sprite, color);
            _windows.Fill(new Rectangle(sprite.X + 14, sprite.Y - 16, sprite.Width - 28, 22), color);
            _windows.Stroke(sprite, enemy.IsActive ? Color.Gold : new Color(52, 26, 30));
            _windows.DrawText($"{(enemy.IsActive ? ">" : " ")} {enemy.Name}", lane.X + 170, y + 6, enemy.IsAlive ? Color.White : Color.Gray, 2, 220);
            _windows.DrawHpBar(new Rectangle(lane.X + 184, y + 34, 176, 12), enemy.CurrentHealth, enemy.MaxHealth);
            y += spacing;
        }
    }

    private void DrawPartyLane(IReadOnlyList<CombatantViewModel> party, Rectangle lane)
    {
        var spacing = Math.Max(64, lane.Height / Math.Max(1, party.Count));
        var y = lane.Y + 18;

        foreach (var member in party.Take(4))
        {
            var sprite = new Rectangle(lane.X + 246, y + 10, 42, 62);
            var color = member.IsAlive ? new Color(54, 148, 184) : new Color(70, 70, 74);

            _windows.Fill(new Rectangle(sprite.X - 10, sprite.Bottom + 6, sprite.Width + 20, 8), new Color(0, 0, 0, 110));
            _windows.Fill(sprite, color);
            _windows.Fill(new Rectangle(sprite.X + 6, sprite.Y - 12, sprite.Width - 12, 18), new Color(236, 192, 126));
            _windows.Stroke(sprite, member.IsActive ? Color.Gold : new Color(26, 42, 60));
            _windows.DrawText(member.IsActive ? "<" : " ", sprite.Right + 16, sprite.Y + 20, Color.Gold, 3);
            y += spacing;
        }
    }

    private void DrawLog(CombatViewModel view, Rectangle bounds)
    {
        _windows.DrawWindow(bounds, "LOG");
        var content = DesktopLayout.Inset(bounds, 26);
        var y = content.Y + 10;

        foreach (var line in view.BattleLog.TakeLast(6))
        {
            _windows.DrawText(line, content.X, y, Color.LightGray, 2, content.Width);
            y += 25;
        }
    }

    private void DrawCommandWindow(CombatViewModel view, Rectangle bounds)
    {
        _windows.DrawWindow(bounds, "COMMAND");
        var content = DesktopLayout.Inset(bounds, 28);
        var options = view.CommandOptions.Count > 0 ? view.CommandOptions :
            view.TargetOptions.Count > 0 ? view.TargetOptions :
            view.ItemOptions.Count > 0 ? view.ItemOptions :
            view.GearOptions;

        if (options.Count > 0)
        {
            _windows.DrawMenu(options, content, 2);
        }
        else
        {
            _windows.DrawText("WAIT", content.X, content.Y + 14, Color.LightGray, 3, content.Width);
        }
    }

    private void DrawPartyStatus(CombatViewModel view, Rectangle bounds)
    {
        _windows.DrawWindow(bounds, "PARTY");
        var content = DesktopLayout.Inset(bounds, 26);
        var y = content.Y + 8;

        foreach (var member in view.Party.Take(4))
        {
            var nameColor = member.Team == CombatTeam.Player && member.IsActive ? Color.Gold : Color.White;
            _windows.DrawText(member.Name, content.X, y, nameColor, 2, 160);
            _windows.DrawText($"{member.CurrentHealth}/{member.MaxHealth}", content.Right - 108, y, Color.White, 2);
            _windows.DrawHpBar(new Rectangle(content.X, y + 22, content.Width, 10), member.CurrentHealth, member.MaxHealth);
            y += 42;
        }
    }
}
