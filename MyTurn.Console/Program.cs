using MyTurn.Application;
using MyTurn.Console.Input;
using MyTurn.Domain;
using MyTurn.Infrastructure;
using Spectre.Console;

namespace MyTurn.Console;

internal static class Program
{
    private const string MenuFooter = "[grey]Move:[/] D-pad/left stick or WASD/arrows  [grey]Select:[/] A/Enter  [grey]Back:[/] B/Esc";
    private const string WorldFooter = "[grey]Move:[/] D-pad/left stick or WASD/arrows  [grey]Hub:[/] B/Start/Esc";
    private const string CombatFooter = "[grey]Navigate:[/] D-pad/left stick or WASD/arrows  [grey]Select:[/] A/Enter  [grey]Back:[/] B/Esc";

    private static void Main()
    {
        var services = SqliteApplicationServices.CreateDefault();
        using var input = GameInput.CreateDefault();
        var state = GameFlowState.MainMenu;
        GameSession? gameSession = null;

        while (state != GameFlowState.Exit)
        {
            switch (state)
            {
                case GameFlowState.MainMenu:
                    state = RunMainMenu(services, input, ref gameSession);
                    break;

                case GameFlowState.CharacterCreation:
                    state = RunCharacterCreation(services, out gameSession);
                    break;

                case GameFlowState.LoadGame:
                    state = RunLoadGame(services, input, out gameSession);
                    break;

                case GameFlowState.CharacterHub:
                    state = gameSession is null
                        ? GameFlowState.MainMenu
                        : RunCharacterHub(services, input, ref gameSession);
                    break;

                default:
                    state = GameFlowState.Exit;
                    break;
            }
        }

        ActorConsoleRenderer.ShowExit();
    }

    private static GameFlowState RunMainMenu(ApplicationServices services, CompositeInputReader input, ref GameSession? gameSession)
    {
        var action = ConsoleMenu.Prompt(
            input,
            "Main Menu",
            Enum.GetValues<MainMenuAction>()
                .Select(action => new ConsoleMenuOption<MainMenuAction>(action.GetDisplayName(), action))
                .ToArray(),
            MenuFooter,
            (options, selected, snapshot) =>
            {
                ActorConsoleRenderer.ShowTitle();
                ConsoleMenu.RenderDefault("Main Menu", options, selected);
                ConsoleMenu.RenderFooter(MenuFooter, snapshot);
            });

        if (action == MainMenuAction.QuickStart)
        {
            gameSession = RunQuickStart(services);
            WaitForContinue(input);
            return GameFlowState.CharacterHub;
        }

        return services.GameFlowService.GetNextState(action);
    }

    private static GameSession RunQuickStart(ApplicationServices services)
    {
        var party = services.QuickStartPartyFactory.CreateParty();
        var gameSession = services.GamePersistence!.CreateSave(party);

        AnsiConsole.Clear();
        ActorConsoleRenderer.ShowCreated(party);

        return gameSession;
    }

    private static GameFlowState RunCharacterCreation(ApplicationServices services, out GameSession gameSession)
    {
        var partySize = ConsolePrompts.PromptForStartingPartySize();
        var actors = new List<Actor>();

        while (actors.Count < partySize)
        {
            AnsiConsole.MarkupLineInterpolated($"[grey]Creating party member {actors.Count + 1} of {partySize}[/]");
            var request = ConsolePrompts.PromptForActor(services.QuickStartPartyFactory.GenerateName());
            ActorConsoleRenderer.ShowCharacterPreview(request);

            if (!ConsolePrompts.ConfirmCharacter())
            {
                AnsiConsole.MarkupLine("[yellow]Let's try that again.[/]");
                continue;
            }

            var actor = services.ActorFactory.Create(request);
            ActorConsoleRenderer.ShowCreated(actor);
            actors.Add(actor);
        }

        var party = services.PartyService.CreateParty(actors);
        gameSession = services.GamePersistence!.CreateSave(party);
        ActorConsoleRenderer.ShowCreated(party);

        return services.GameFlowService.GetStateAfterCharacterCreation();
    }

    private static GameFlowState RunLoadGame(ApplicationServices services, CompositeInputReader input, out GameSession? gameSession)
    {
        var saves = services.GamePersistence!.ListSaves();

        if (saves.Count == 0)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[yellow]No saves found.[/]");
            WaitForContinue(input);
            gameSession = null;
            return GameFlowState.MainMenu;
        }

        var selectedSave = ConsoleMenu.PromptOrCancel(
            input,
            "Load Game",
            saves.Select(save => new ConsoleMenuOption<SaveSlotSummary>(
                    save.Name,
                    save,
                    save.LastPlayedAtUtc.ToLocalTime().ToString("g")))
                .ToArray(),
            MenuFooter);

        if (selectedSave is null)
        {
            gameSession = null;
            return GameFlowState.MainMenu;
        }

        gameSession = services.GamePersistence.LoadSave(selectedSave.Id);

        if (gameSession.ActiveWorldSession is not null)
        {
            services.WorldSessionService.SetActiveSession(gameSession.Party, gameSession.ActiveWorldSession);
        }

        AnsiConsole.Clear();
        ActorConsoleRenderer.ShowLoaded(gameSession.Party);
        WaitForContinue(input);

        return GameFlowState.CharacterHub;
    }

    private static GameFlowState RunCharacterHub(ApplicationServices services, CompositeInputReader input, ref GameSession gameSession)
    {
        var party = gameSession.Party;
        var action = ConsoleMenu.Prompt(
            input,
            "Hub",
            Enum.GetValues<CharacterHubAction>()
                .Select(action => new ConsoleMenuOption<CharacterHubAction>(action.GetDisplayName(), action))
                .ToArray(),
            MenuFooter,
            (options, selected, snapshot) =>
            {
                ActorConsoleRenderer.ShowHub(party, options.Select(option => option.Label).ToArray(), selected, snapshot);
            });

        switch (action)
        {
            case CharacterHubAction.ExploreWorld:
                RunExploreWorld(services, input, ref gameSession);
                break;

            case CharacterHubAction.FightEncounter:
                RunCombat(services, input, party);
                services.GamePersistence!.Save(gameSession);
                break;

            case CharacterHubAction.ViewParty:
                ShowScreen(input, () => ActorConsoleRenderer.ShowParty(party));
                break;

            case CharacterHubAction.ManageParty:
                ManageParty(services, input, party);
                services.GamePersistence!.Save(gameSession);
                break;

            case CharacterHubAction.ViewCharacter:
                if (PromptForPartyMember(input, party.Roster, "Choose a party member") is { } character)
                {
                    ShowScreen(input, () => ActorConsoleRenderer.ShowCharacter(character));
                }

                break;

            case CharacterHubAction.ViewInventory:
                ShowScreen(input, () => ActorConsoleRenderer.ShowInventory(party));
                break;

            case CharacterHubAction.ViewStats:
                if (PromptForPartyMember(input, party.Roster, "Choose a party member") is { } statsMember)
                {
                    ShowScreen(input, () => ActorConsoleRenderer.ShowStats(statsMember));
                }

                break;

            case CharacterHubAction.ViewSkills:
                if (PromptForPartyMember(input, party.Roster, "Choose a party member") is { } skillsMember)
                {
                    ShowScreen(input, () => ActorConsoleRenderer.ShowSkills(skillsMember));
                }

                break;

            case CharacterHubAction.ViewEquipment:
                if (PromptForPartyMember(input, party.Roster, "Choose a party member") is { } equipmentMember)
                {
                    ShowScreen(input, () => ActorConsoleRenderer.ShowEquipment(equipmentMember));
                }

                break;

            case CharacterHubAction.UseItem:
                ShowScreen(input, ActorConsoleRenderer.ShowConsumablesAreCombatOnly);
                break;

            case CharacterHubAction.EquipGear:
                EquipGearFromInventory(services, input, party);
                services.GamePersistence!.Save(gameSession);
                break;
        }

        return services.GameFlowService.GetNextState(action);
    }

    private static BattleOutcome RunCombat(
        ApplicationServices services,
        CompositeInputReader input,
        Party party,
        Encounter? encounterOverride = null)
    {
        var encounter = encounterOverride ?? services.EncounterGenerator.Generate();
        var random = new SeededRandomSource(encounter.Seed);
        var state = services.CombatService.StartEncounter(party, encounter);
        var battleLog = new List<string>();

        AddBattleLog(battleLog, $"[red]Encounter![/] {encounter.Enemies.Count} enemies appear.");

        while (!state.IsComplete)
        {
            var turnOrder = services.CombatService.GetTurnOrder(state);

            foreach (var combatant in turnOrder)
            {
                if (!combatant.IsAlive || state.IsComplete)
                {
                    continue;
                }

                if (combatant.Team == CombatTeam.Player)
                {
                    RunPlayerTurn(services, input, state, combatant, random, battleLog);
                }
                else
                {
                    RunEnemyTurn(services, state, combatant, random, battleLog);
                    ActorConsoleRenderer.ShowBattleScreen(state, combatant, battleLog);
                    Thread.Sleep(350);
                }
            }
        }

        var outcome = state.IsVictory
            ? services.CombatService.CompleteVictory(state, random)
            : services.CombatService.CompleteDefeat(state);

        ActorConsoleRenderer.ShowBattleScreen(state, null, battleLog);
        ActorConsoleRenderer.ShowBattleOutcome(outcome);
        WaitForContinue(input);

        return outcome;
    }

    private static void RunExploreWorld(ApplicationServices services, CompositeInputReader input, ref GameSession gameSession)
    {
        var party = gameSession.Party;
        var session = services.WorldSessionService.GetOrCreate(party);
        var currentSession = gameSession with { ActiveWorldSession = session };
        services.GamePersistence!.Save(currentSession);
        var repeatController = new MovementRepeatController();
        var autosave = new AutosaveThrottle();
        var keepExploring = true;
        ExplorationResult? lastResult = null;
        var needsRender = true;
        string? lastControllerName = null;
        string? lastHeldCommands = null;

        void SaveSession()
        {
            currentSession = currentSession with { ActiveWorldSession = session };
            services.GamePersistence!.Save(currentSession);
        }

        while (keepExploring && !session.IsCompleted)
        {
            var now = DateTimeOffset.UtcNow;
            var snapshot = input.Poll();
            var heldCommands = string.Join(",", snapshot.HeldCommands.OrderBy(command => command));

            if (!string.Equals(lastControllerName, snapshot.ControllerName, StringComparison.Ordinal) ||
                !string.Equals(lastHeldCommands, heldCommands, StringComparison.Ordinal))
            {
                needsRender = true;
                lastControllerName = snapshot.ControllerName;
                lastHeldCommands = heldCommands;
            }

            if (snapshot.IsPressed(GameCommand.Cancel) || snapshot.IsPressed(GameCommand.Menu))
            {
                autosave.ForceSave(now, SaveSession);
                gameSession = currentSession;
                return;
            }

            if (repeatController.TryConsume(snapshot, now, out var moveCommand) &&
                TryGetDirection(moveCommand, out var direction))
            {
                var result = services.ExplorationService.TryMove(party, session, direction);
                lastResult = result;
                needsRender = true;

                switch (result.State)
                {
                    case ExplorationState.Moved:
                        autosave.MarkChanged();
                        break;
                    case ExplorationState.Blocked:
                        break;
                    case ExplorationState.TreasureFound:
                        autosave.ForceSave(now, SaveSession);
                        break;
                    case ExplorationState.EnemyEncounter when result.Encounter is not null:
                        ActorConsoleRenderer.ShowWorld(party, session, services.MinimapService.CreateSnapshot(session), result, snapshot);
                        var outcome = RunCombat(services, input, party, result.Encounter);

                        if (outcome.OutcomeType == BattleOutcomeType.Victory)
                        {
                            services.ExplorationService.ClearEnemyRoom(session);
                            autosave.ForceSave(now, SaveSession);
                        }
                        else
                        {
                            autosave.ForceSave(now, SaveSession);
                            keepExploring = false;
                        }

                        break;
                    case ExplorationState.ExitReached:
                        ActorConsoleRenderer.ShowWorld(party, session, services.MinimapService.CreateSnapshot(session), result, snapshot);
                        ActorConsoleRenderer.ShowWorldCompleted(session);
                        autosave.ForceSave(now, SaveSession);
                        WaitForContinue(input);
                        keepExploring = false;
                        break;
                }
            }

            autosave.TrySave(now, SaveSession);

            if (needsRender)
            {
                ActorConsoleRenderer.ShowWorld(party, session, services.MinimapService.CreateSnapshot(session), lastResult, snapshot);
                needsRender = false;
            }

            Thread.Sleep(16);
        }

        gameSession = currentSession;
    }

    private static void RunPlayerTurn(
        ApplicationServices services,
        CompositeInputReader input,
        CombatState state,
        Combatant combatant,
        IRandomSource random,
        List<string> battleLog)
    {
        var turnConsumed = false;

        while (!turnConsumed && !state.IsComplete)
        {
            var action = PromptForCombatAction(input, state, combatant, battleLog);

            if (action is null)
            {
                AddBattleLog(battleLog, "[grey]Choose an action.[/]");
                continue;
            }

            switch (action)
            {
                case CombatActionType.Attack:
                    var target = PromptForEnemyTarget(input, state, combatant, battleLog);

                    if (target is null)
                    {
                        AddBattleLog(battleLog, "[grey]Attack cancelled.[/]");
                        break;
                    }

                    var damage = services.CombatService.Attack(combatant, target, random);
                    AddBattleLog(battleLog, FormatDamageLog(damage));
                    turnConsumed = true;
                    break;

                case CombatActionType.UseConsumable:
                    var itemId = PromptForConsumable(input, state, combatant, battleLog);

                    if (itemId is null)
                    {
                        AddBattleLog(battleLog, "[grey]Item cancelled.[/]");
                        break;
                    }

                    var healing = services.CombatService.UseConsumable(state, itemId);
                    AddBattleLog(battleLog, FormatHealingLog(healing));
                    turnConsumed = true;
                    break;

                case CombatActionType.Defend:
                    services.CombatService.Defend(combatant);
                    AddBattleLog(battleLog, $"[yellow]{Markup.Escape(combatant.Name)}[/] guards.");
                    turnConsumed = true;
                    break;

                case CombatActionType.ChangeEquipment:
                    var equipment = PromptForEquipment(input, state, combatant, battleLog);

                    if (equipment is null)
                    {
                        AddBattleLog(battleLog, "[grey]Equipment cancelled.[/]");
                        break;
                    }

                    services.CombatService.ChangeEquipment(state, combatant, equipment);
                    AddBattleLog(battleLog, $"[green]{Markup.Escape(combatant.Name)}[/] equips [yellow]{Markup.Escape(equipment.Name)}[/].");
                    break;

                case CombatActionType.ViewCombatants:
                    AddBattleLog(battleLog, "[grey]You study the battlefield.[/]");
                    break;
            }
        }
    }

    private static CombatActionType? PromptForCombatAction(
        CompositeInputReader input,
        CombatState state,
        Combatant combatant,
        IReadOnlyList<string> battleLog)
    {
        var options = Enum.GetValues<CombatActionType>()
            .Select(action => new ConsoleMenuOption<CombatActionType>(action.GetDisplayName(), action))
            .ToArray();

        return ConsoleMenu.PromptOrCancel(
            input,
            "Command",
            options,
            CombatFooter,
            (menuOptions, selected, snapshot) =>
            {
                ActorConsoleRenderer.ShowBattleScreen(state, combatant, battleLog, menuOptions.Select(option => option.Label).ToArray(), selected, snapshot);
            });
    }

    private static Combatant? PromptForEnemyTarget(
        CompositeInputReader input,
        CombatState state,
        Combatant combatant,
        IReadOnlyList<string> battleLog)
    {
        var options = state.LivingEnemies
            .Select(enemy => new ConsoleMenuOption<Combatant>(
                $"{enemy.Name} {enemy.CurrentHealth}/{enemy.MaxHealth} HP",
                enemy))
            .ToArray();

        return ConsoleMenu.PromptOrCancel(
            input,
            "Target",
            options,
            CombatFooter,
            (menuOptions, selected, snapshot) =>
            {
                ActorConsoleRenderer.ShowBattleScreen(state, combatant, battleLog, menuOptions.Select(option => option.Label).ToArray(), selected, snapshot);
            });
    }

    private static string? PromptForConsumable(
        CompositeInputReader input,
        CombatState state,
        Combatant combatant,
        IReadOnlyList<string> battleLog)
    {
        var options = state.Party.Inventory.Items
            .Where(stack => stack.Item is ConsumableDefinition)
            .Select(stack => new ConsoleMenuOption<string>(
                $"{stack.Item.Name} x{stack.Quantity}",
                stack.Item.Id))
            .ToArray();

        if (options.Length == 0)
        {
            return null;
        }

        return ConsoleMenu.PromptOrCancel(
            input,
            "Item",
            options,
            CombatFooter,
            (menuOptions, selected, snapshot) =>
            {
                ActorConsoleRenderer.ShowBattleScreen(state, combatant, battleLog, menuOptions.Select(option => option.Label).ToArray(), selected, snapshot);
            });
    }

    private static IEquipmentItem? PromptForEquipment(
        CompositeInputReader input,
        CombatState state,
        Combatant combatant,
        IReadOnlyList<string> battleLog)
    {
        var options = state.Party.Inventory.Items
            .Where(stack => stack.Item is IEquipmentItem)
            .Select(stack => new ConsoleMenuOption<IEquipmentItem>(
                $"{stack.Item.Name} ({((IEquipmentItem)stack.Item).Slot.GetDisplayName()})",
                (IEquipmentItem)stack.Item))
            .ToArray();

        if (options.Length == 0)
        {
            return null;
        }

        return ConsoleMenu.PromptOrCancel(
            input,
            "Gear",
            options,
            CombatFooter,
            (menuOptions, selected, snapshot) =>
            {
                ActorConsoleRenderer.ShowBattleScreen(state, combatant, battleLog, menuOptions.Select(option => option.Label).ToArray(), selected, snapshot);
            });
    }

    private static void RunEnemyTurn(
        ApplicationServices services,
        CombatState state,
        Combatant enemy,
        IRandomSource random,
        List<string> battleLog)
    {
        var result = services.CombatService.ResolveEnemyTurn(state, enemy, random);

        if (result.DamageResult is not null)
        {
            AddBattleLog(battleLog, FormatDamageLog(result.DamageResult));
            return;
        }

        AddBattleLog(battleLog, $"[yellow]{Markup.Escape(enemy.Name)}[/] guards.");
    }

    private static void AddBattleLog(List<string> battleLog, string message)
    {
        const int maxLogEntries = 8;

        battleLog.Add(message);

        if (battleLog.Count > maxLogEntries)
        {
            battleLog.RemoveAt(0);
        }
    }

    private static string FormatDamageLog(DamageResult result)
    {
        var critical = result.IsCritical ? " [bold yellow]Critical![/]" : string.Empty;
        var defeated = result.TargetDefeated ? " [red]Defeated![/]" : string.Empty;

        return $"[yellow]{Markup.Escape(result.Attacker.Name)}[/] hits [yellow]{Markup.Escape(result.Target.Name)}[/] for [red]{result.Damage}[/].{critical}{defeated}";
    }

    private static string FormatHealingLog(HealingResult result)
    {
        return $"[green]{Markup.Escape(result.Target.Name)}[/] uses [yellow]{Markup.Escape(result.Consumable.Name)}[/] and heals [green]{result.AmountHealed}[/].";
    }

    private static void EquipGearFromInventory(ApplicationServices services, CompositeInputReader input, Party party)
    {
        var equipmentOptions = party.Inventory.Items
            .Where(stack => stack.Item is IEquipmentItem)
            .Select(stack => new ConsoleMenuOption<IEquipmentItem>(
                $"{stack.Item.Name} ({((IEquipmentItem)stack.Item).Slot.GetDisplayName()})",
                (IEquipmentItem)stack.Item))
            .ToArray();

        if (equipmentOptions.Length == 0)
        {
            ShowScreen(input, () => ActorConsoleRenderer.ShowNoAvailableItems("No equipment is available."));
            return;
        }

        var item = ConsoleMenu.PromptOrCancel(input, "Choose Gear", equipmentOptions, MenuFooter);

        if (item is null)
        {
            return;
        }

        var actor = PromptForPartyMember(input, party.ActiveMembers, "Equip to");

        if (actor is null)
        {
            return;
        }

        services.EquipmentService.Equip(actor, item);
        ShowScreen(input, () => ActorConsoleRenderer.ShowGearChanged(item));
    }

    private static void ManageParty(ApplicationServices services, CompositeInputReader input, Party party)
    {
        var options = new List<ConsoleMenuOption<PartyManagementAction>>
        {
            new("View Party", PartyManagementAction.ViewParty)
        };

        if (party.ActiveMembers.Count > Party.MinActiveMembers)
        {
            options.Add(new ConsoleMenuOption<PartyManagementAction>("Move Active To Reserve", PartyManagementAction.MoveActiveToReserve));
        }

        if (party.ReserveMembers.Count > 0 && party.ActiveMembers.Count < Party.MaxActiveMembers)
        {
            options.Add(new ConsoleMenuOption<PartyManagementAction>("Move Reserve To Active", PartyManagementAction.MoveReserveToActive));
        }

        var action = ConsoleMenu.PromptOrCancel(input, "Manage Party", options, MenuFooter);

        switch (action)
        {
            case PartyManagementAction.MoveActiveToReserve:
                if (PromptForPartyMember(input, party.ActiveMembers, "Move to reserve") is { } activeMember)
                {
                    services.PartyService.MoveToReserve(party, activeMember.Id);
                    ShowScreen(input, () => ActorConsoleRenderer.ShowParty(party));
                }

                break;

            case PartyManagementAction.MoveReserveToActive:
                if (PromptForPartyMember(input, party.ReserveMembers, "Move to active party") is { } reserveMember)
                {
                    services.PartyService.MoveToActive(party, reserveMember.Id);
                    ShowScreen(input, () => ActorConsoleRenderer.ShowParty(party));
                }

                break;

            case PartyManagementAction.ViewParty:
                ShowScreen(input, () => ActorConsoleRenderer.ShowParty(party));
                break;
        }
    }

    private static Actor? PromptForPartyMember(CompositeInputReader input, IEnumerable<Actor> members, string title)
    {
        var options = members
            .Select(member => new ConsoleMenuOption<Actor>(
                member.Name,
                member,
                $"{member.CharacterClass.GetDisplayName()} {member.Stats[StatType.Health].CurrentValue}/{member.Stats[StatType.Health].MaxValue} HP"))
            .ToArray();

        if (options.Length == 0)
        {
            return null;
        }

        return ConsoleMenu.PromptOrCancel(input, title, options, MenuFooter);
    }

    private static void ShowScreen(CompositeInputReader input, Action render)
    {
        AnsiConsole.Clear();
        render();
        WaitForContinue(input);
    }

    private static void WaitForContinue(CompositeInputReader input)
    {
        ConsoleMenu.RenderFooter("[grey]Continue:[/] A/Enter  [grey]Back:[/] B/Esc", input.Poll());

        while (true)
        {
            var snapshot = input.Poll();

            if (snapshot.IsPressed(GameCommand.Confirm) ||
                snapshot.IsPressed(GameCommand.Cancel) ||
                snapshot.IsPressed(GameCommand.Menu))
            {
                return;
            }

            Thread.Sleep(16);
        }
    }

    private static bool TryGetDirection(GameCommand command, out Direction direction)
    {
        switch (command)
        {
            case GameCommand.MoveUp:
                direction = Direction.North;
                return true;
            case GameCommand.MoveDown:
                direction = Direction.South;
                return true;
            case GameCommand.MoveLeft:
                direction = Direction.West;
                return true;
            case GameCommand.MoveRight:
                direction = Direction.East;
                return true;
            default:
                direction = default;
                return false;
        }
    }
}
