using MyTurn.Application;
using MyTurn.Domain;
using Spectre.Console;

namespace MyTurn.Console;

internal enum PartyManagementAction
{
    ViewParty,
    MoveActiveToReserve,
    MoveReserveToActive
}

internal static class ConsolePrompts
{
    private sealed record MenuOption<T>(string Label, T Value);

    public static MainMenuAction PromptForMainMenuAction()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<MainMenuAction>()
                .Title("Main Menu")
                .UseConverter(action => action.GetDisplayName())
                .AddChoices(Enum.GetValues<MainMenuAction>()));
    }

    public static CreateActorRequest PromptForActor(string generatedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(generatedName);

        var nameMode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Name")
                .AddChoices("Enter Name", $"Use {generatedName}"));
        var name = nameMode == "Enter Name"
            ? AnsiConsole.Prompt(
                new TextPrompt<string>("What is your name?")
                    .PromptStyle("green")
                    .ValidationErrorMessage("[red]That's not a valid name[/]")
                    .Validate(name => !string.IsNullOrWhiteSpace(name)))
            : generatedName;

        var age = AnsiConsole.Prompt(
            new TextPrompt<int>("What is your age?")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]That's not a valid age[/]")
                .Validate(age => age > 17));

        var gender = AnsiConsole.Prompt(
            new SelectionPrompt<Gender>()
                .Title("Select your gender:")
                .UseConverter(gender => gender.GetDisplayName())
                .AddChoices(Enum.GetValues<Gender>()));

        var species = AnsiConsole.Prompt(
            new SelectionPrompt<Species>()
                .Title("Select your species:")
                .UseConverter(species => species.GetDisplayName())
                .PageSize(10)
                .AddChoices(Enum.GetValues<Species>()));

        var characterClass = AnsiConsole.Prompt(
            new SelectionPrompt<CharacterClass>()
                .Title("Select your character class:")
                .UseConverter(characterClass => characterClass.GetDisplayName())
                .AddChoices(Enum.GetValues<CharacterClass>()));

        return new CreateActorRequest(name, age, gender, species, characterClass);
    }

    public static int PromptForStartingPartySize()
    {
        return AnsiConsole.Prompt(
            new TextPrompt<int>("How many starting party members? [grey](1-4)[/]")
                .PromptStyle("green")
                .DefaultValue(1)
                .ValidationErrorMessage("[red]Party size must be between 1 and 4.[/]")
                .Validate(size => size is >= Party.MinActiveMembers and <= Party.MaxActiveMembers));
    }

    public static bool ConfirmCharacter()
    {
        return AnsiConsole.Prompt(
            new ConfirmationPrompt("Start the game with this character?"));
    }

    public static SaveSlotSummary PromptForSaveSlot(IEnumerable<SaveSlotSummary> saves)
    {
        var options = saves
            .Select(save => new MenuOption<SaveSlotSummary>(
                $"{save.Name} - {save.LastPlayedAtUtc.ToLocalTime():g}",
                save))
            .ToArray();

        return AnsiConsole.Prompt(
            new SelectionPrompt<MenuOption<SaveSlotSummary>>()
                .Title("Load Game")
                .UseConverter(option => option.Label)
                .AddChoices(options))
            .Value;
    }

    public static CharacterHubAction PromptForCharacterHubAction()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<CharacterHubAction>()
                .Title("What would you like to do?")
                .UseConverter(action => action.GetDisplayName())
                .AddChoices(Enum.GetValues<CharacterHubAction>()));
    }

    public static Actor PromptForPartyMember(IEnumerable<Actor> members, string title)
    {
        var choices = members.ToArray();

        if (choices.Length == 0)
        {
            throw new InvalidOperationException("No party members are available.");
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<Actor>()
                .Title(title)
                .UseConverter(member => $"{member.Name} - {member.CharacterClass.GetDisplayName()} ({member.Stats[StatType.Health].CurrentValue}/{member.Stats[StatType.Health].MaxValue} HP)")
                .AddChoices(choices));
    }

    public static PartyManagementAction PromptForPartyManagementAction(Party party)
    {
        var options = new List<MenuOption<PartyManagementAction>>
        {
            new("View party", PartyManagementAction.ViewParty)
        };

        if (party.ActiveMembers.Count > Party.MinActiveMembers)
        {
            options.Add(new MenuOption<PartyManagementAction>("Move active member to reserve", PartyManagementAction.MoveActiveToReserve));
        }

        if (party.ReserveMembers.Count > 0 && party.ActiveMembers.Count < Party.MaxActiveMembers)
        {
            options.Add(new MenuOption<PartyManagementAction>("Move reserve member to active party", PartyManagementAction.MoveReserveToActive));
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<MenuOption<PartyManagementAction>>()
                .Title("Manage Party")
                .UseConverter(option => option.Label)
                .AddChoices(options))
            .Value;
    }

    public static int? PromptForEncounterSeed()
    {
        var seedMode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Encounter seed")
                .AddChoices("Random Seed", "Enter Seed"));

        if (seedMode == "Random Seed")
        {
            return null;
        }

        return AnsiConsole.Prompt(
            new TextPrompt<int>("Seed:")
                .PromptStyle("green"));
    }

    public static int? PromptForWorldSeed()
    {
        var seedMode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("World seed")
                .AddChoices("Random Seed", "Enter Seed"));

        if (seedMode == "Random Seed")
        {
            return null;
        }

        return AnsiConsole.Prompt(
            new TextPrompt<int>("Seed:")
                .PromptStyle("green"));
    }

    public static CombatActionType PromptForCombatAction()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<CombatActionType>()
                .Title("Choose your action:")
                .UseConverter(action => action.GetDisplayName())
                .AddChoices(Enum.GetValues<CombatActionType>()));
    }

    public static Combatant PromptForEnemyTarget(IEnumerable<Combatant> enemies)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<Combatant>()
                .Title("Choose a target:")
                .UseConverter(enemy => $"{enemy.Name} ({enemy.CurrentHealth}/{enemy.MaxHealth} HP)")
                .AddChoices(enemies));
    }

    public static bool TryPromptForConsumable(Inventory inventory, out string? itemId)
    {
        var options = inventory.Items
            .Where(stack => stack.Item is ConsumableDefinition)
            .Select(stack => new MenuOption<string?>($"{stack.Item.Name} x{stack.Quantity}", stack.Item.Id))
            .Append(new MenuOption<string?>("Cancel", null))
            .ToArray();

        if (options.Length == 1)
        {
            itemId = null;
            return false;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<MenuOption<string?>>()
                .Title("Choose a consumable:")
                .UseConverter(option => option.Label)
                .AddChoices(options));

        itemId = selected.Value;
        return itemId is not null;
    }

    public static bool TryPromptForEquipmentItem(Inventory inventory, out IEquipmentItem? item)
    {
        var options = inventory.Items
            .Where(stack => stack.Item is IEquipmentItem)
            .Select(stack => new MenuOption<IEquipmentItem?>($"{stack.Item.Name} ({((IEquipmentItem)stack.Item).Slot.GetDisplayName()})", (IEquipmentItem)stack.Item))
            .Append(new MenuOption<IEquipmentItem?>("Cancel", null))
            .ToArray();

        if (options.Length == 1)
        {
            item = null;
            return false;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<MenuOption<IEquipmentItem?>>()
                .Title("Choose gear:")
                .UseConverter(option => option.Label)
                .AddChoices(options));

        item = selected.Value;
        return item is not null;
    }
}
