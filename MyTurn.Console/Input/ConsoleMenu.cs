using Spectre.Console;

namespace MyTurn.Console.Input;

public sealed record ConsoleMenuOption<T>(string Label, T Value, string? Detail = null);

public static class ConsoleMenu
{
    public static T Prompt<T>(
        CompositeInputReader input,
        string title,
        IReadOnlyList<ConsoleMenuOption<T>> options,
        string footer,
        Action<IReadOnlyList<ConsoleMenuOption<T>>, int, InputSnapshot>? render = null)
    {
        return PromptOrCancel(input, title, options, footer, render)
            ?? throw new InvalidOperationException("A required menu was cancelled.");
    }

    public static T? PromptOrCancel<T>(
        CompositeInputReader input,
        string title,
        IReadOnlyList<ConsoleMenuOption<T>> options,
        string footer,
        Action<IReadOnlyList<ConsoleMenuOption<T>>, int, InputSnapshot>? render = null)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (options.Count == 0)
        {
            throw new ArgumentException("Menu requires at least one option.", nameof(options));
        }

        var controller = new MenuController(options.Count);
        var needsRender = true;
        string? lastControllerName = null;
        string? lastHeldCommands = null;

        while (true)
        {
            var snapshot = input.Poll();
            var interaction = controller.Handle(snapshot, DateTimeOffset.UtcNow);
            var heldCommands = GetCommandSignature(snapshot);

            if (interaction.Kind == MenuInteractionKind.Confirmed)
            {
                return options[interaction.SelectedIndex].Value;
            }

            if (interaction.Kind == MenuInteractionKind.Cancelled)
            {
                return default;
            }

            if (interaction.Kind == MenuInteractionKind.Moved ||
                !string.Equals(lastControllerName, snapshot.ControllerName, StringComparison.Ordinal) ||
                !string.Equals(lastHeldCommands, heldCommands, StringComparison.Ordinal))
            {
                needsRender = true;
            }

            if (needsRender)
            {
                AnsiConsole.Clear();

                if (render is not null)
                {
                    render(options, controller.SelectedIndex, snapshot);
                }
                else
                {
                    RenderDefault(title, options, controller.SelectedIndex);
                    RenderFooter(footer, snapshot);
                }

                lastControllerName = snapshot.ControllerName;
                lastHeldCommands = heldCommands;
                needsRender = false;
            }

            Thread.Sleep(16);
        }
    }

    public static void RenderDefault<T>(string title, IReadOnlyList<ConsoleMenuOption<T>> options, int selectedIndex)
    {
        var table = new Table()
            .Title(title)
            .Border(TableBorder.Rounded)
            .AddColumn(" ")
            .AddColumn("Option")
            .AddColumn("Details");

        for (var i = 0; i < options.Count; i++)
        {
            var marker = i == selectedIndex ? "[bold yellow]>[/]" : " ";
            var label = i == selectedIndex ? $"[bold yellow]{Markup.Escape(options[i].Label)}[/]" : Markup.Escape(options[i].Label);
            table.AddRow(marker, label, Markup.Escape(options[i].Detail ?? string.Empty));
        }

        AnsiConsole.Write(table);
    }

    public static void RenderFooter(string text, InputSnapshot snapshot)
    {
        var controllerText = snapshot.HasController
            ? $"[green]{Markup.Escape(snapshot.ControllerName!)}[/]"
            : "[grey]Keyboard[/]";
        var commandText = snapshot.HeldCommands.Count > 0
            ? $"{Environment.NewLine}[grey]Commands:[/] {Markup.Escape(string.Join(", ", snapshot.HeldCommands))}"
            : string.Empty;
        AnsiConsole.Write(
            new Panel($"{text}{Environment.NewLine}[grey]Input:[/] {controllerText}{commandText}")
                .BorderColor(Color.Grey));
    }

    private static string GetCommandSignature(InputSnapshot snapshot)
    {
        return string.Join(",", snapshot.HeldCommands.OrderBy(command => command));
    }
}
