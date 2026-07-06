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
                    state = RunMainMenu(services);
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

    private static GameFlowState RunMainMenu(ApplicationServices services)
    {
        ActorConsoleRenderer.ShowTitle();
        var action = ConsolePrompts.PromptForMainMenuAction();

        return services.GameFlowService.GetNextState(action);
    }

    private static GameFlowState RunCharacterCreation(ApplicationServices services, out GameSession gameSession)
    {
        while (true)
        {
            var request = ConsolePrompts.PromptForActor();
            ActorConsoleRenderer.ShowCharacterPreview(request);

            if (!ConsolePrompts.ConfirmCharacter())
            {
                AnsiConsole.MarkupLine("[yellow]Let's try that again.[/]");
                continue;
            }

            var actor = services.ActorFactory.Create(request);
            gameSession = services.GamePersistence!.CreateSave(actor);
            ActorConsoleRenderer.ShowCreated(actor);

            return services.GameFlowService.GetStateAfterCharacterCreation();
        }
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
            services.WorldSessionService.SetActiveSession(gameSession.Actor, gameSession.ActiveWorldSession);
        }

        ActorConsoleRenderer.ShowLoaded(gameSession.Actor);

        return GameFlowState.CharacterHub;
    }

    private static GameFlowState RunCharacterHub(ApplicationServices services, ref GameSession gameSession)
    {
        var actor = gameSession.Actor;
        var action = ConsolePrompts.PromptForCharacterHubAction();

        switch (action)
        {
            case CharacterHubAction.ViewCharacter:
                ActorConsoleRenderer.ShowCharacter(actor);
                break;

            case CharacterHubAction.ExploreWorld:
                RunExploreWorld(services, ref gameSession);
                break;

            case CharacterHubAction.FightEncounter:
                RunCombat(services, actor);
                services.GamePersistence!.Save(gameSession);
                break;

            case CharacterHubAction.ViewInventory:
                ActorConsoleRenderer.ShowInventory(actor);
                break;

            case CharacterHubAction.ViewStats:
                ActorConsoleRenderer.ShowStats(actor);
                break;

            case CharacterHubAction.ViewSkills:
                ActorConsoleRenderer.ShowSkills(actor);
                break;

            case CharacterHubAction.ViewEquipment:
                ActorConsoleRenderer.ShowEquipment(actor);
                break;

            case CharacterHubAction.UseItem:
                ActorConsoleRenderer.ShowConsumablesAreCombatOnly();
                break;

            case CharacterHubAction.EquipGear:
                EquipGearFromInventory(services, actor);
                services.GamePersistence!.Save(gameSession);
                break;
        }

        return services.GameFlowService.GetNextState(action);
    }

    private static BattleOutcome RunCombat(ApplicationServices services, Actor actor, Encounter? encounterOverride = null)
    {
        var encounter = encounterOverride ?? services.EncounterGenerator.Generate(seed: ConsolePrompts.PromptForEncounterSeed());
        var random = new SeededRandomSource(encounter.Seed);
        var state = services.CombatService.StartEncounter(actor, encounter);

        ActorConsoleRenderer.ShowEncounter(encounter);

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
                    RunPlayerTurn(services, state, random);
                }
                else
                {
                    RunEnemyTurn(services, state, combatant, random);
                }
            }
        }

        var outcome = state.IsVictory
            ? services.CombatService.CompleteVictory(state, random)
            : services.CombatService.CompleteDefeat(state);

        ActorConsoleRenderer.ShowBattleOutcome(outcome);

        return outcome;
    }

    private static void RunExploreWorld(ApplicationServices services, ref GameSession gameSession)
    {
        var actor = gameSession.Actor;
        var seed = services.WorldSessionService.HasActiveSession(actor)
            ? null
            : ConsolePrompts.PromptForWorldSeed();
        var session = services.WorldSessionService.GetOrCreate(actor, seed);
        gameSession = gameSession with { ActiveWorldSession = session };
        services.GamePersistence!.Save(gameSession);
        var keepExploring = true;

        while (keepExploring && !session.IsCompleted)
        {
            ActorConsoleRenderer.ShowWorld(actor, session, services.MinimapService.CreateSnapshot(session));
            var key = System.Console.ReadKey(intercept: true);

            if (IsExploreExitKey(key.Key))
            {
                return;
            }

            if (!TryGetDirection(key.Key, out var direction))
            {
                continue;
            }

            var result = services.ExplorationService.TryMove(actor, session, direction);
            ActorConsoleRenderer.ShowWorldMessage(result);
            services.GamePersistence!.Save(gameSession);

            switch (result.State)
            {
                case ExplorationState.EnemyEncounter when result.Encounter is not null:
                    var outcome = RunCombat(services, actor, result.Encounter);

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
                    ActorConsoleRenderer.ShowWorldCompleted(session);
                    services.GamePersistence.Save(gameSession);
                    keepExploring = false;
                    break;
            }
        }
    }

    private static void RunPlayerTurn(ApplicationServices services, CombatState state, IRandomSource random)
    {
        var turnConsumed = false;

        while (!turnConsumed && !state.IsComplete)
        {
            ActorConsoleRenderer.ShowCombatants(state);
            var action = ConsolePrompts.PromptForCombatAction();

            switch (action)
            {
                case CombatActionType.Attack:
                    var target = ConsolePrompts.PromptForEnemyTarget(state.LivingEnemies);
                    var damage = services.CombatService.Attack(state.PlayerCombatant, target, random);
                    ActorConsoleRenderer.ShowDamage(damage);
                    turnConsumed = true;
                    break;

                case CombatActionType.UseConsumable:
                    if (!ConsolePrompts.TryPromptForConsumable(state.Player.Inventory, out var itemId))
                    {
                        ActorConsoleRenderer.ShowNoAvailableItems("No consumables are available.");
                        break;
                    }

                    var healing = services.CombatService.UseConsumable(state, itemId!);
                    ActorConsoleRenderer.ShowHealing(healing);
                    turnConsumed = true;
                    break;

                case CombatActionType.Defend:
                    services.CombatService.Defend(state.PlayerCombatant);
                    ActorConsoleRenderer.ShowDefend(state.PlayerCombatant);
                    turnConsumed = true;
                    break;

                case CombatActionType.ChangeEquipment:
                    if (!ConsolePrompts.TryPromptForEquipmentItem(state.Player.Inventory, out var item))
                    {
                        ActorConsoleRenderer.ShowNoAvailableItems("No equipment is available.");
                        break;
                    }

                    services.CombatService.ChangeEquipment(state, item!);
                    ActorConsoleRenderer.ShowGearChanged(item!);
                    break;

                case CombatActionType.ViewCombatants:
                    ActorConsoleRenderer.ShowCombatants(state);
                    break;
            }
        }
    }

    private static void RunEnemyTurn(ApplicationServices services, CombatState state, Combatant enemy, IRandomSource random)
    {
        var result = services.CombatService.ResolveEnemyTurn(state, enemy, random);

        if (result.DamageResult is not null)
        {
            ActorConsoleRenderer.ShowDamage(result.DamageResult);
            return;
        }

        ActorConsoleRenderer.ShowDefend(enemy);
    }

    private static void EquipGearFromInventory(ApplicationServices services, Actor actor)
    {
        if (!ConsolePrompts.TryPromptForEquipmentItem(actor.Inventory, out var item))
        {
            ActorConsoleRenderer.ShowNoAvailableItems("No equipment is available.");
            return;
        }

        services.EquipmentService.Equip(actor, item!);
        ActorConsoleRenderer.ShowGearChanged(item!);
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
