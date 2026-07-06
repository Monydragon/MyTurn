using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Infrastructure;
using Spectre.Console;

namespace MyTurn.Console;

internal static class Program
{
    private static void Main()
    {
        var services = SqliteApplicationServices.CreateDefault();
        var state = GameFlowState.MainMenu;
        GameSession? gameSession = null;

        while (state != GameFlowState.Exit)
        {
            switch (state)
            {
                case GameFlowState.MainMenu:
                    state = RunMainMenu(services, ref gameSession);
                    break;

                case GameFlowState.CharacterCreation:
                    state = RunCharacterCreation(services, out gameSession);
                    break;

                case GameFlowState.LoadGame:
                    state = RunLoadGame(services, out gameSession);
                    break;

                case GameFlowState.CharacterHub:
                    state = gameSession is null
                        ? GameFlowState.MainMenu
                        : RunCharacterHub(services, ref gameSession);
                    break;

                default:
                    state = GameFlowState.Exit;
                    break;
            }
        }

        ActorConsoleRenderer.ShowExit();
    }

    private static GameFlowState RunMainMenu(ApplicationServices services, ref GameSession? gameSession)
    {
        ActorConsoleRenderer.ShowTitle();
        var action = ConsolePrompts.PromptForMainMenuAction();

        if (action == MainMenuAction.QuickStart)
        {
            gameSession = RunQuickStart(services);
            return GameFlowState.CharacterHub;
        }

        return services.GameFlowService.GetNextState(action);
    }

    private static GameSession RunQuickStart(ApplicationServices services)
    {
        var party = services.QuickStartPartyFactory.CreateParty();
        var gameSession = services.GamePersistence!.CreateSave(party);

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

    private static GameFlowState RunLoadGame(ApplicationServices services, out GameSession? gameSession)
    {
        var saves = services.GamePersistence!.ListSaves();

        if (saves.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No saves found.[/]");
            gameSession = null;
            return GameFlowState.MainMenu;
        }

        var selectedSave = ConsolePrompts.PromptForSaveSlot(saves);
        gameSession = services.GamePersistence.LoadSave(selectedSave.Id);

        if (gameSession.ActiveWorldSession is not null)
        {
            services.WorldSessionService.SetActiveSession(gameSession.Party, gameSession.ActiveWorldSession);
        }

        ActorConsoleRenderer.ShowLoaded(gameSession.Party);

        return GameFlowState.CharacterHub;
    }

    private static GameFlowState RunCharacterHub(ApplicationServices services, ref GameSession gameSession)
    {
        var party = gameSession.Party;
        var action = ConsolePrompts.PromptForCharacterHubAction();

        switch (action)
        {
            case CharacterHubAction.ExploreWorld:
                RunExploreWorld(services, ref gameSession);
                break;

            case CharacterHubAction.FightEncounter:
                RunCombat(services, party);
                services.GamePersistence!.Save(gameSession);
                break;

            case CharacterHubAction.ViewParty:
                ActorConsoleRenderer.ShowParty(party);
                break;

            case CharacterHubAction.ManageParty:
                ManageParty(services, party);
                services.GamePersistence!.Save(gameSession);
                break;

            case CharacterHubAction.ViewCharacter:
                ActorConsoleRenderer.ShowCharacter(ConsolePrompts.PromptForPartyMember(party.Roster, "Choose a party member:"));
                break;

            case CharacterHubAction.ViewInventory:
                ActorConsoleRenderer.ShowInventory(party);
                break;

            case CharacterHubAction.ViewStats:
                ActorConsoleRenderer.ShowStats(ConsolePrompts.PromptForPartyMember(party.Roster, "Choose a party member:"));
                break;

            case CharacterHubAction.ViewSkills:
                ActorConsoleRenderer.ShowSkills(ConsolePrompts.PromptForPartyMember(party.Roster, "Choose a party member:"));
                break;

            case CharacterHubAction.ViewEquipment:
                ActorConsoleRenderer.ShowEquipment(ConsolePrompts.PromptForPartyMember(party.Roster, "Choose a party member:"));
                break;

            case CharacterHubAction.UseItem:
                ActorConsoleRenderer.ShowConsumablesAreCombatOnly();
                break;

            case CharacterHubAction.EquipGear:
                EquipGearFromInventory(services, party);
                services.GamePersistence!.Save(gameSession);
                break;
        }

        return services.GameFlowService.GetNextState(action);
    }

    private static BattleOutcome RunCombat(ApplicationServices services, Party party, Encounter? encounterOverride = null)
    {
        var encounter = encounterOverride ?? services.EncounterGenerator.Generate(seed: ConsolePrompts.PromptForEncounterSeed());
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
                    RunPlayerTurn(services, state, combatant, random, battleLog);
                }
                else
                {
                    RunEnemyTurn(services, state, combatant, random, battleLog);
                }
            }
        }

        var outcome = state.IsVictory
            ? services.CombatService.CompleteVictory(state, random)
            : services.CombatService.CompleteDefeat(state);

        ActorConsoleRenderer.ShowBattleScreen(state, null, battleLog);
        ActorConsoleRenderer.ShowBattleOutcome(outcome);

        return outcome;
    }

    private static void RunExploreWorld(ApplicationServices services, ref GameSession gameSession)
    {
        var party = gameSession.Party;
        var session = services.WorldSessionService.GetOrCreate(party);
        gameSession = gameSession with { ActiveWorldSession = session };
        services.GamePersistence!.Save(gameSession);
        var keepExploring = true;
        ExplorationResult? lastResult = null;

        while (keepExploring && !session.IsCompleted)
        {
            ActorConsoleRenderer.ShowWorld(party, session, services.MinimapService.CreateSnapshot(session), lastResult);
            var key = System.Console.ReadKey(intercept: true);

            if (IsExploreExitKey(key.Key))
            {
                return;
            }

            if (!TryGetDirection(key.Key, out var direction))
            {
                continue;
            }

            var result = services.ExplorationService.TryMove(party, session, direction);
            lastResult = result;
            services.GamePersistence!.Save(gameSession);

            switch (result.State)
            {
                case ExplorationState.EnemyEncounter when result.Encounter is not null:
                    ActorConsoleRenderer.ShowWorld(party, session, services.MinimapService.CreateSnapshot(session), result);
                    var outcome = RunCombat(services, party, result.Encounter);

                    if (outcome.OutcomeType == BattleOutcomeType.Victory)
                    {
                        services.ExplorationService.ClearEnemyRoom(session);
                        services.GamePersistence.Save(gameSession);
                    }
                    else
                    {
                        services.GamePersistence.Save(gameSession);
                        keepExploring = false;
                    }

                    break;

                case ExplorationState.ExitReached:
                    ActorConsoleRenderer.ShowWorld(party, session, services.MinimapService.CreateSnapshot(session), result);
                    ActorConsoleRenderer.ShowWorldCompleted(session);
                    services.GamePersistence.Save(gameSession);
                    keepExploring = false;
                    break;
            }
        }
    }

    private static void RunPlayerTurn(
        ApplicationServices services,
        CombatState state,
        Combatant combatant,
        IRandomSource random,
        List<string> battleLog)
    {
        var turnConsumed = false;

        while (!turnConsumed && !state.IsComplete)
        {
            ActorConsoleRenderer.ShowBattleScreen(state, combatant, battleLog);
            var action = ConsolePrompts.PromptForCombatAction();

            switch (action)
            {
                case CombatActionType.Attack:
                    var target = ConsolePrompts.PromptForEnemyTarget(state.LivingEnemies);
                    var damage = services.CombatService.Attack(combatant, target, random);
                    AddBattleLog(battleLog, FormatDamageLog(damage));
                    turnConsumed = true;
                    break;

                case CombatActionType.UseConsumable:
                    if (!ConsolePrompts.TryPromptForConsumable(state.Party.Inventory, out var itemId))
                    {
                        AddBattleLog(battleLog, "[yellow]No consumables are available.[/]");
                        break;
                    }

                    var healing = services.CombatService.UseConsumable(state, itemId!);
                    AddBattleLog(battleLog, FormatHealingLog(healing));
                    turnConsumed = true;
                    break;

                case CombatActionType.Defend:
                    services.CombatService.Defend(combatant);
                    AddBattleLog(battleLog, $"[yellow]{Markup.Escape(combatant.Name)}[/] guards.");
                    turnConsumed = true;
                    break;

                case CombatActionType.ChangeEquipment:
                    if (!ConsolePrompts.TryPromptForEquipmentItem(state.Party.Inventory, out var item))
                    {
                        AddBattleLog(battleLog, "[yellow]No equipment is available.[/]");
                        break;
                    }

                    var equipment = item!;
                    services.CombatService.ChangeEquipment(state, combatant, equipment);
                    AddBattleLog(battleLog, $"[green]{Markup.Escape(combatant.Name)}[/] equips [yellow]{Markup.Escape(equipment.Name)}[/].");
                    break;

                case CombatActionType.ViewCombatants:
                    AddBattleLog(battleLog, "[grey]You study the battlefield.[/]");
                    break;
            }
        }
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

    private static void EquipGearFromInventory(ApplicationServices services, Party party)
    {
        if (!ConsolePrompts.TryPromptForEquipmentItem(party.Inventory, out var item))
        {
            ActorConsoleRenderer.ShowNoAvailableItems("No equipment is available.");
            return;
        }

        var actor = ConsolePrompts.PromptForPartyMember(party.ActiveMembers, "Equip to:");
        services.EquipmentService.Equip(actor, item!);
        ActorConsoleRenderer.ShowGearChanged(item!);
    }

    private static void ManageParty(ApplicationServices services, Party party)
    {
        var action = ConsolePrompts.PromptForPartyManagementAction(party);

        switch (action)
        {
            case PartyManagementAction.MoveActiveToReserve:
                var activeMember = ConsolePrompts.PromptForPartyMember(party.ActiveMembers, "Move to reserve:");
                services.PartyService.MoveToReserve(party, activeMember.Id);
                ActorConsoleRenderer.ShowParty(party);
                break;

            case PartyManagementAction.MoveReserveToActive:
                var reserveMember = ConsolePrompts.PromptForPartyMember(party.ReserveMembers, "Move to active party:");
                services.PartyService.MoveToActive(party, reserveMember.Id);
                ActorConsoleRenderer.ShowParty(party);
                break;

            case PartyManagementAction.ViewParty:
                ActorConsoleRenderer.ShowParty(party);
                break;
        }
    }

    private static bool TryGetDirection(ConsoleKey key, out Direction direction)
    {
        switch (key)
        {
            case ConsoleKey.W:
            case ConsoleKey.UpArrow:
                direction = Direction.North;
                return true;

            case ConsoleKey.S:
            case ConsoleKey.DownArrow:
                direction = Direction.South;
                return true;

            case ConsoleKey.A:
            case ConsoleKey.LeftArrow:
                direction = Direction.West;
                return true;

            case ConsoleKey.D:
            case ConsoleKey.RightArrow:
                direction = Direction.East;
                return true;

            default:
                direction = default;
                return false;
        }
    }

    private static bool IsExploreExitKey(ConsoleKey key)
    {
        return key is ConsoleKey.Q or ConsoleKey.Escape;
    }
}
