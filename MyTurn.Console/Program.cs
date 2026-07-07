using MyTurn.Console.Input;
using MyTurn.Infrastructure;
using MyTurn.Presentation;
using MyTurn.Presentation.Input;

namespace MyTurn.Console;

internal static class Program
{
    private static readonly TimeSpan FrameTime = TimeSpan.FromMilliseconds(8);

    private static void Main()
    {
        var services = SqliteApplicationServices.CreateDefault();
        using var input = GameInput.CreateDefault();
        var client = new GameClient(services);
        string? lastSignature = null;

        while (client.CurrentView.ScreenKind != ScreenKind.Exit)
        {
            var snapshot = input.Poll();
            client.Update(snapshot, FrameTime);

            var view = client.CurrentView;
            var signature = CreateRenderSignature(view, snapshot);

            if (!string.Equals(signature, lastSignature, StringComparison.Ordinal))
            {
                ActorConsoleRenderer.Show(view, snapshot);
                lastSignature = signature;
            }

            Thread.Sleep(FrameTime);
        }

        ActorConsoleRenderer.ShowExit();
    }

    private static string CreateRenderSignature(GameViewModel view, InputSnapshot snapshot)
    {
        var commands = string.Join(",", snapshot.HeldCommands.OrderBy(command => command));

        return view switch
        {
            MainMenuViewModel mainMenu => $"{mainMenu.ScreenKind}|{commands}|{Selected(mainMenu.Options)}",
            LoadGameViewModel loadGame => $"{loadGame.ScreenKind}|{commands}|{Selected(loadGame.Options)}|{loadGame.Options.Count}",
            HubViewModel hub => $"{hub.ScreenKind}|{commands}|{Selected(hub.Options)}|{hub.Party.Steps}|{hub.Party.Currency}",
            WorldViewModel world => $"{world.ScreenKind}|{commands}|{world.CurrentPosition}|{world.LatestEvent}|{world.HasPendingEncounter}|{world.Party.Steps}",
            CombatViewModel combat => $"{combat.ScreenKind}|{commands}|{combat.ActiveCombatantName}|{Selected(combat.CommandOptions)}|{Selected(combat.TargetOptions)}|{Selected(combat.ItemOptions)}|{Selected(combat.GearOptions)}|{string.Join(";", combat.BattleLog)}|{combat.OutcomeTitle}",
            PartyViewModel party => $"{party.ScreenKind}|{commands}|{party.Members.Count}|{party.Party.Steps}",
            InventoryViewModel inventory => $"{inventory.ScreenKind}|{commands}|{inventory.Currency}|{inventory.Items.Count}",
            MessageViewModel message => $"{message.ScreenKind}|{commands}|{message.Title}|{message.Message}",
            _ => $"{view.ScreenKind}|{commands}"
        };
    }

    private static string Selected(IReadOnlyList<MenuOptionViewModel> options)
    {
        var selected = options
            .Select((option, index) => (option, index))
            .FirstOrDefault(item => item.option.IsSelected);

        return selected.option is null ? "-1" : selected.index.ToString();
    }
}
