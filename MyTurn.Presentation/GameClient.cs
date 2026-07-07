using System.Text.Json;
using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Presentation.Input;

namespace MyTurn.Presentation;

public sealed class GameClient
{
    private const int WorldCameraColumns = 17;
    private const int WorldCameraRows = 11;
    private const int MaxBattleLogEntries = 8;

    private readonly ApplicationServices _services;
    private readonly TileMapDefinition? _fallbackTileMap;
    private readonly IWorldTileMapProvider? _tileMapProvider;
    private readonly AutosaveThrottle _autosave = new();

    private ScreenKind _screen = ScreenKind.MainMenu;
    private GameSession? _session;
    private MenuController _mainMenu = new(Enum.GetValues<MainMenuAction>().Length);
    private MenuController _loadMenu = new(1);
    private MenuController _hubMenu = new(Enum.GetValues<CharacterHubAction>().Length);
    private ExplorationResult? _latestWorldEvent;
    private ExplorationResult? _pendingEncounter;
    private WorldObject? _pendingWorldObject;
    private Direction _facingDirection = Direction.South;
    private bool _worldDirectionHeldLastFrame;
    private CombatState? _combatState;
    private SeededRandomSource? _combatRandom;
    private IReadOnlyList<Combatant> _turnOrder = [];
    private int _turnIndex;
    private Combatant? _activeCombatant;
    private CombatMode _combatMode = CombatMode.None;
    private MenuController _combatActionMenu = new(Enum.GetValues<CombatActionType>().Length);
    private MenuController _targetMenu = new(1);
    private MenuController _itemMenu = new(1);
    private MenuController _gearMenu = new(1);
    private readonly List<string> _battleLog = [];
    private BattleOutcome? _combatOutcome;
    private bool _combatReturnsToWorld;
    private MessageViewModel? _message;

    public GameClient(
        ApplicationServices services,
        TileMapDefinition? tileMap = null,
        IWorldTileMapProvider? tileMapProvider = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _fallbackTileMap = tileMap;
        _tileMapProvider = tileMapProvider;
    }

    public GameViewModel CurrentView => BuildCurrentView();

    public void StartNewQuickGame()
    {
        var party = _services.QuickStartPartyFactory.CreateParty();
        _session = _services.GamePersistence?.CreateSave(party)
            ?? new GameSession(Guid.NewGuid(), party, null);
        _services.GamePersistence?.Save(_session);
        _screen = ScreenKind.Hub;
        _hubMenu = new MenuController(Enum.GetValues<CharacterHubAction>().Length);
    }

    public void LoadSave(Guid saveSlotId)
    {
        if (_services.GamePersistence is null)
        {
            ShowMessage("Load Game", "Persistence is not configured for this build.", ScreenKind.MainMenu);
            return;
        }

        _session = _services.GamePersistence.LoadSave(saveSlotId);

        if (_session.ActiveWorldSession is not null)
        {
            _services.WorldSessionService.SetActiveSession(_session.Party, _session.ActiveWorldSession);
        }

        _screen = ScreenKind.Hub;
        _hubMenu = new MenuController(Enum.GetValues<CharacterHubAction>().Length);
    }

    public void Update(InputSnapshot input, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(input);

        switch (_screen)
        {
            case ScreenKind.MainMenu:
                UpdateMainMenu(input);
                break;
            case ScreenKind.LoadGame:
                UpdateLoadGame(input);
                break;
            case ScreenKind.Hub:
                UpdateHub(input);
                break;
            case ScreenKind.World:
                UpdateWorld(input, elapsed);
                break;
            case ScreenKind.Combat:
                UpdateCombat(input);
                break;
            case ScreenKind.Party:
            case ScreenKind.Inventory:
            case ScreenKind.Message:
                UpdateDismissibleScreen(input);
                break;
        }
    }

    private void UpdateMainMenu(InputSnapshot input)
    {
        var interaction = _mainMenu.Handle(input, DateTimeOffset.UtcNow);

        if (interaction.Kind != MenuInteractionKind.Confirmed)
        {
            return;
        }

        var action = Enum.GetValues<MainMenuAction>()[interaction.SelectedIndex];

        switch (action)
        {
            case MainMenuAction.QuickStart:
            case MainMenuAction.NewGame:
                StartNewQuickGame();
                break;
            case MainMenuAction.LoadGame:
                OpenLoadGame();
                break;
            case MainMenuAction.Exit:
                _screen = ScreenKind.Exit;
                break;
        }
    }

    private void OpenLoadGame()
    {
        var saves = _services.GamePersistence?.ListSaves() ?? [];

        if (saves.Count == 0)
        {
            ShowMessage("Load Game", "No saves found.", ScreenKind.MainMenu);
            return;
        }

        _loadMenu = new MenuController(saves.Count);
        _screen = ScreenKind.LoadGame;
    }

    private void UpdateLoadGame(InputSnapshot input)
    {
        if (input.IsPressed(GameCommand.Cancel))
        {
            _screen = ScreenKind.MainMenu;
            return;
        }

        var saves = _services.GamePersistence?.ListSaves() ?? [];

        if (saves.Count == 0)
        {
            ShowMessage("Load Game", "No saves found.", ScreenKind.MainMenu);
            return;
        }

        var interaction = _loadMenu.Handle(input, DateTimeOffset.UtcNow);

        if (interaction.Kind == MenuInteractionKind.Confirmed)
        {
            LoadSave(saves[interaction.SelectedIndex].Id);
        }
        else if (interaction.Kind == MenuInteractionKind.Cancelled)
        {
            _screen = ScreenKind.MainMenu;
        }
    }

    private void UpdateHub(InputSnapshot input)
    {
        if (_session is null)
        {
            _screen = ScreenKind.MainMenu;
            return;
        }

        var interaction = _hubMenu.Handle(input, DateTimeOffset.UtcNow);

        if (interaction.Kind != MenuInteractionKind.Confirmed)
        {
            return;
        }

        var action = Enum.GetValues<CharacterHubAction>()[interaction.SelectedIndex];

        switch (action)
        {
            case CharacterHubAction.ExploreWorld:
                OpenWorld();
                break;
            case CharacterHubAction.FightEncounter:
                StartCombat(_services.EncounterGenerator.Generate(), returnsToWorld: false);
                break;
            case CharacterHubAction.ViewParty:
            case CharacterHubAction.ViewCharacter:
            case CharacterHubAction.ViewStats:
            case CharacterHubAction.ViewSkills:
            case CharacterHubAction.ViewEquipment:
            case CharacterHubAction.ManageParty:
            case CharacterHubAction.EquipGear:
                _screen = ScreenKind.Party;
                break;
            case CharacterHubAction.ViewInventory:
            case CharacterHubAction.UseItem:
                _screen = ScreenKind.Inventory;
                break;
            case CharacterHubAction.BackToMainMenu:
                SaveCurrentSession(force: true);
                _screen = ScreenKind.MainMenu;
                break;
            case CharacterHubAction.Exit:
                SaveCurrentSession(force: true);
                _screen = ScreenKind.Exit;
                break;
        }
    }

    private void OpenWorld()
    {
        if (_session is null)
        {
            _screen = ScreenKind.MainMenu;
            return;
        }

        var world = _services.WorldSessionService.GetOrCreate(_session.Party);
        _session = _session with { ActiveWorldSession = world };
        _latestWorldEvent = null;
        _pendingEncounter = null;
        _pendingWorldObject = null;
        _worldDirectionHeldLastFrame = false;
        RestorePendingEncounterIfNeeded(world);
        _autosave.MarkChanged();
        _screen = ScreenKind.World;
    }

    private void RestorePendingEncounterIfNeeded(WorldSession world)
    {
        if (_session is null ||
            world.CurrentRoom.RoomType != RoomType.Enemy ||
            world.CurrentRoom.IsCleared)
        {
            return;
        }

        var result = _services.ExplorationService.EnterCurrentRoom(_session.Party, world);

        if (result.State != ExplorationState.EnemyEncounter)
        {
            return;
        }

        _pendingEncounter = result with
        {
            Message = "Enemies are here. Confirm to start battle, or cancel to return to the hub."
        };
        _pendingWorldObject = world.ActiveObjectsAt(world.CurrentPosition)
            .FirstOrDefault(obj => obj.ObjectType == WorldObjectType.Enemy);
        _latestWorldEvent = _pendingEncounter;
    }

    private void UpdateWorld(InputSnapshot input, TimeSpan elapsed)
    {
        if (_session?.ActiveWorldSession is null)
        {
            _screen = ScreenKind.Hub;
            return;
        }

        var hasWorldDirectionHeld = HasWorldDirectionHeld(input);

        try
        {
            if (input.IsPressed(GameCommand.Cancel) || input.IsPressed(GameCommand.Menu))
            {
                SaveCurrentSession(force: true);
                _screen = ScreenKind.Hub;
                return;
            }

            if (_pendingEncounter is not null)
            {
                if (input.IsPressed(GameCommand.Confirm))
                {
                    StartCombat(_pendingEncounter.Encounter, returnsToWorld: true);
                }
                else if (hasWorldDirectionHeld)
                {
                    _latestWorldEvent = _pendingEncounter with
                    {
                        Message = "Enemies block the room. Confirm to start battle, or cancel to return to the hub."
                    };
                }

                return;
            }

            if (_pendingWorldObject is not null && input.IsPressed(GameCommand.Confirm))
            {
                ResolveWorldObjectInteraction(_pendingWorldObject);
                return;
            }

            if (!TryStartWorldMovement(input))
            {
                if (input.IsPressed(GameCommand.Confirm))
                {
                    TryCreateFacingInteractionPrompt();
                }
            }
        }
        finally
        {
            _worldDirectionHeldLastFrame = hasWorldDirectionHeld;
        }
    }

    private bool TryStartWorldMovement(InputSnapshot input)
    {
        if (_session?.ActiveWorldSession is null || !TryGetWorldStepDirection(input, out var direction))
        {
            return false;
        }

        var result = _services.ExplorationService.TryMove(_session.Party, _session.ActiveWorldSession, direction);
        _facingDirection = direction;
        _pendingWorldObject = null;
        _pendingEncounter = null;

        if (result.State == ExplorationState.Blocked)
        {
            _latestWorldEvent = result;
            TryCreateBlockedInteractionPrompt(_session.ActiveWorldSession.CurrentPosition.Move(direction));
            return false;
        }

        _latestWorldEvent = result;
        ResolveCurrentTileObjectEvents();

        if (_pendingEncounter is null && _pendingWorldObject is null)
        {
            switch (result.State)
            {
                case ExplorationState.Moved:
                    _autosave.MarkChanged();
                    break;
                case ExplorationState.TreasureFound:
                    SaveCurrentSession(force: true);
                    break;
                case ExplorationState.EnemyEncounter when result.Encounter is not null:
                    _pendingEncounter = result with
                    {
                        Message = "Enemies are here. Confirm to start battle, or cancel to return to the hub."
                    };
                    _latestWorldEvent = _pendingEncounter;
                    _autosave.MarkChanged();
                    break;
                case ExplorationState.ExitReached:
                    SaveCurrentSession(force: true);
                    ShowMessage("World Complete", result.Message, ScreenKind.Hub);
                    break;
            }
        }

        return true;
    }

    private void ResolveCurrentTileObjectEvents()
    {
        if (_session?.ActiveWorldSession is null)
        {
            return;
        }

        var world = _session.ActiveWorldSession;
        var objects = world.ActiveObjectsAt(world.CurrentPosition);

        foreach (var hazard in objects.Where(obj => obj.ObjectType == WorldObjectType.Hazard))
        {
            TriggerHazard(hazard);
            return;
        }

        var enemy = objects.FirstOrDefault(obj => obj.ObjectType == WorldObjectType.Enemy);

        if (enemy is not null)
        {
            var encounter = _services.EncounterGenerator.Generate(seed: enemy.EncounterSeed);
            _pendingWorldObject = enemy;
            _pendingEncounter = new ExplorationResult(
                ExplorationState.EnemyEncounter,
                world.CurrentRoom,
                encounter,
                LootReward.Empty,
                "Enemies are here. Confirm to start battle, or cancel to return to the hub.");
            _latestWorldEvent = _pendingEncounter;
            _autosave.MarkChanged();
            return;
        }

        var promptObject = objects.FirstOrDefault(obj =>
            obj.ObjectType is WorldObjectType.Treasure or
                WorldObjectType.Key or
                WorldObjectType.Pickup or
                WorldObjectType.Exit or
                WorldObjectType.Npc or
                WorldObjectType.Sign);

        if (promptObject is not null)
        {
            SetWorldObjectPrompt(promptObject);
        }
        else
        {
            _autosave.MarkChanged();
        }
    }

    private bool TryCreateFacingInteractionPrompt()
    {
        if (_session?.ActiveWorldSession is null)
        {
            return false;
        }

        var target = _session.ActiveWorldSession.CurrentPosition.Move(_facingDirection);
        var obj = _session.ActiveWorldSession.ActiveObjectsAt(target)
            .FirstOrDefault(IsPromptableObject);

        if (obj is null)
        {
            return false;
        }

        SetWorldObjectPrompt(obj);
        return true;
    }

    private bool TryCreateBlockedInteractionPrompt(WorldPosition target)
    {
        if (_session?.ActiveWorldSession is null)
        {
            return false;
        }

        var obj = _session.ActiveWorldSession.BlockingObjectAt(target);

        if (obj is null)
        {
            return false;
        }

        SetWorldObjectPrompt(obj);
        return true;
    }

    private void SetWorldObjectPrompt(WorldObject worldObject)
    {
        _pendingWorldObject = worldObject;
        var prompt = CreateWorldObjectPrompt(worldObject);
        _latestWorldEvent = new ExplorationResult(
            ExplorationState.Moved,
            _session!.ActiveWorldSession!.CurrentRoom,
            null,
            LootReward.Empty,
            $"{prompt.Title}: {prompt.Message}");
    }

    private void ResolveWorldObjectInteraction(WorldObject worldObject)
    {
        if (_session?.ActiveWorldSession is null)
        {
            return;
        }

        switch (worldObject.ObjectType)
        {
            case WorldObjectType.Treasure:
                ClaimObjectReward(worldObject, markCollected: true);
                break;
            case WorldObjectType.Pickup:
                ClaimObjectReward(worldObject, markCollected: true);
                break;
            case WorldObjectType.Key:
                worldObject.MarkCollected();
                _latestWorldEvent = MessageResult(ReadPayloadString(worldObject, "message", "You found a key."));
                break;
            case WorldObjectType.Exit:
                worldObject.MarkTriggered();
                _session.ActiveWorldSession.MarkCompleted();
                _latestWorldEvent = MessageResult("You found the exit and completed this world.");
                SaveCurrentSession(force: true);
                ShowMessage("World Complete", _latestWorldEvent.Message, ScreenKind.Hub);
                _pendingWorldObject = null;
                return;
            case WorldObjectType.Door:
                worldObject.MarkOpened();
                _latestWorldEvent = MessageResult("The door opens.");
                break;
            case WorldObjectType.LockedDoor:
                if (HasKeyFor(worldObject))
                {
                    worldObject.MarkOpened();
                    _latestWorldEvent = MessageResult("The lock clicks open.");
                }
                else
                {
                    _latestWorldEvent = MessageResult(ReadPayloadString(worldObject, "message", "The door is locked."));
                }

                break;
            case WorldObjectType.Npc:
            case WorldObjectType.Sign:
                _latestWorldEvent = MessageResult(ReadPayloadString(worldObject, "message", "There is nothing else here."));
                break;
            case WorldObjectType.BlockingProp:
                _latestWorldEvent = MessageResult("Something blocks the way.");
                break;
        }

        _pendingWorldObject = null;
        _autosave.MarkChanged();
    }

    private void TriggerHazard(WorldObject worldObject)
    {
        var damage = ReadPayloadInt(worldObject, "damage", 3);

        foreach (var member in _session!.Party.ActiveMembers)
        {
            var health = member.Stats[StatType.Health];
            health.SetBaseValue(Math.Max(1, health.BaseValue - damage));
        }

        worldObject.MarkTriggered();
        _latestWorldEvent = MessageResult(ReadPayloadString(worldObject, "message", "A trap catches the party."));
        _autosave.MarkChanged();
    }

    private void ClaimObjectReward(WorldObject worldObject, bool markCollected)
    {
        var currency = ReadPayloadInt(worldObject, "currency", worldObject.ObjectType == WorldObjectType.Treasure ? 18 : 0);
        var itemId = ReadPayloadString(worldObject, "itemId", worldObject.ObjectType == WorldObjectType.Pickup ? "small-healing-potion" : string.Empty);
        var quantity = ReadPayloadInt(worldObject, "quantity", 1);

        if (currency > 0)
        {
            _session!.Party.Inventory.AddCurrency(currency);
        }

        if (!string.IsNullOrWhiteSpace(itemId) && _services.ItemDefinitions.TryGet(itemId, out var item))
        {
            _session!.Party.Inventory.Add(item, Math.Max(1, quantity));
        }

        if (markCollected)
        {
            worldObject.MarkCollected();
        }

        _latestWorldEvent = MessageResult(ReadPayloadString(worldObject, "message", "You found something useful."));
    }

    private bool HasKeyFor(WorldObject lockedDoor)
    {
        var keyId = ReadPayloadString(lockedDoor, "keyId", "bronze-key");

        return _session?.ActiveWorldSession?.Objects.Any(obj =>
            obj.ObjectType == WorldObjectType.Key &&
            obj.State == WorldObjectState.Collected &&
            string.Equals(ReadPayloadString(obj, "keyId", "bronze-key"), keyId, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private WorldInteractionPromptViewModel CreateWorldObjectPrompt(WorldObject worldObject)
    {
        return worldObject.ObjectType switch
        {
            WorldObjectType.Treasure => new(worldObject.Id, "Treasure", "Treasure", ReadPayloadString(worldObject, "message", "Open the chest?"), true),
            WorldObjectType.Key => new(worldObject.Id, "Key", "Key", ReadPayloadString(worldObject, "message", "Pick up the key?"), true),
            WorldObjectType.Pickup => new(worldObject.Id, "Pickup", "Pickup", ReadPayloadString(worldObject, "message", "Pick this up?"), true),
            WorldObjectType.Exit => new(worldObject.Id, "Exit", "Exit", "Leave this world?", true),
            WorldObjectType.Door => new(worldObject.Id, "Door", "Door", "Open the door?", true),
            WorldObjectType.LockedDoor => new(worldObject.Id, "Locked Door", "Locked Door", HasKeyFor(worldObject) ? "Unlock the door?" : ReadPayloadString(worldObject, "message", "The door is locked."), true),
            WorldObjectType.Npc => new(worldObject.Id, "NPC", "Talk", ReadPayloadString(worldObject, "message", "Talk?"), true),
            WorldObjectType.Sign => new(worldObject.Id, "Sign", "Read", ReadPayloadString(worldObject, "message", "Read the sign?"), true),
            WorldObjectType.BlockingProp => new(worldObject.Id, "Blocked", "Blocked", "Something blocks the way.", false),
            _ => new(worldObject.Id, worldObject.ObjectType.GetDisplayName(), worldObject.ObjectType.GetDisplayName(), "Interact?", true)
        };
    }

    private ExplorationResult MessageResult(string message)
    {
        return new ExplorationResult(
            ExplorationState.Moved,
            _session!.ActiveWorldSession!.CurrentRoom,
            null,
            LootReward.Empty,
            message);
    }

    private static bool IsPromptableObject(WorldObject worldObject)
    {
        return worldObject.ObjectType is WorldObjectType.Treasure or
            WorldObjectType.Exit or
            WorldObjectType.Door or
            WorldObjectType.LockedDoor or
            WorldObjectType.Key or
            WorldObjectType.Pickup or
            WorldObjectType.Npc or
            WorldObjectType.Sign or
            WorldObjectType.BlockingProp;
    }

    private static string ReadPayloadString(WorldObject worldObject, string key, string fallback)
    {
        if (string.IsNullOrWhiteSpace(worldObject.PayloadJson))
        {
            return fallback;
        }

        using var document = JsonDocument.Parse(worldObject.PayloadJson);

        return document.RootElement.TryGetProperty(key, out var value)
            ? value.ToString()
            : fallback;
    }

    private static int ReadPayloadInt(WorldObject worldObject, string key, int fallback)
    {
        var raw = ReadPayloadString(worldObject, key, fallback.ToString());

        return int.TryParse(raw, out var value) ? value : fallback;
    }

    private void StartCombat(Encounter? encounter, bool returnsToWorld)
    {
        if (_session is null || encounter is null)
        {
            _screen = ScreenKind.Hub;
            return;
        }

        _combatState = _services.CombatService.StartEncounter(_session.Party, encounter);
        _combatRandom = new SeededRandomSource(encounter.Seed);
        _turnOrder = [];
        _turnIndex = 0;
        _activeCombatant = null;
        _combatMode = CombatMode.None;
        _combatActionMenu = new MenuController(Enum.GetValues<CombatActionType>().Length);
        _targetMenu = new MenuController(Math.Max(1, _combatState.LivingEnemies.Count));
        _itemMenu = new MenuController(Math.Max(1, GetConsumableItems().Count));
        _gearMenu = new MenuController(Math.Max(1, GetEquipmentItems().Count));
        _battleLog.Clear();
        AddBattleLog($"Encounter! {encounter.Enemies.Count} enemies appear.");
        _combatOutcome = null;
        _combatReturnsToWorld = returnsToWorld;
        _screen = ScreenKind.Combat;
    }

    private void UpdateCombat(InputSnapshot input)
    {
        if (_session is null || _combatState is null || _combatRandom is null)
        {
            _screen = ScreenKind.Hub;
            return;
        }

        if (_combatOutcome is not null)
        {
            if (input.IsPressed(GameCommand.Confirm) ||
                input.IsPressed(GameCommand.Cancel) ||
                input.IsPressed(GameCommand.Menu))
            {
                FinishCombatScreen();
            }

            return;
        }

        if (_combatState.IsComplete)
        {
            CompleteCombat();
            return;
        }

        EnsureActiveCombatant();

        if (_activeCombatant?.Team != CombatTeam.Player)
        {
            return;
        }

        switch (_combatMode)
        {
            case CombatMode.Action:
                UpdateCombatAction(input);
                break;
            case CombatMode.Target:
                UpdateTargetSelection(input);
                break;
            case CombatMode.Item:
                UpdateItemSelection(input);
                break;
            case CombatMode.Gear:
                UpdateGearSelection(input);
                break;
        }
    }

    private void EnsureActiveCombatant()
    {
        if (_combatState is null || _combatRandom is null || _activeCombatant is not null || _combatOutcome is not null)
        {
            return;
        }

        while (!_combatState.IsComplete)
        {
            if (_turnIndex >= _turnOrder.Count)
            {
                _turnOrder = _services.CombatService.GetTurnOrder(_combatState);
                _turnIndex = 0;
            }

            if (_turnOrder.Count == 0)
            {
                CompleteCombat();
                return;
            }

            var combatant = _turnOrder[_turnIndex++];

            if (!combatant.IsAlive)
            {
                continue;
            }

            _activeCombatant = combatant;

            if (combatant.Team == CombatTeam.Player)
            {
                _combatMode = CombatMode.Action;
                _combatActionMenu = new MenuController(Enum.GetValues<CombatActionType>().Length);
                return;
            }

            var enemyResult = _services.CombatService.ResolveEnemyTurn(_combatState, combatant, _combatRandom);

            if (enemyResult.DamageResult is not null)
            {
                AddBattleLog(FormatDamageLog(enemyResult.DamageResult));
            }
            else
            {
                AddBattleLog($"{combatant.Name} guards.");
            }

            _activeCombatant = null;
            _combatMode = CombatMode.None;
            return;
        }

        CompleteCombat();
    }

    private void UpdateCombatAction(InputSnapshot input)
    {
        if (_combatState is null)
        {
            return;
        }

        var interaction = _combatActionMenu.Handle(input, DateTimeOffset.UtcNow);

        if (interaction.Kind != MenuInteractionKind.Confirmed)
        {
            return;
        }

        var action = Enum.GetValues<CombatActionType>()[interaction.SelectedIndex];

        switch (action)
        {
            case CombatActionType.Attack:
                _targetMenu = new MenuController(Math.Max(1, _combatState.LivingEnemies.Count));
                _combatMode = CombatMode.Target;
                break;
            case CombatActionType.UseConsumable:
                if (GetConsumableItems().Count == 0)
                {
                    AddBattleLog("No consumables are available.");
                    break;
                }

                _itemMenu = new MenuController(GetConsumableItems().Count);
                _combatMode = CombatMode.Item;
                break;
            case CombatActionType.Defend:
                _services.CombatService.Defend(_activeCombatant!);
                AddBattleLog($"{_activeCombatant!.Name} guards.");
                EndPlayerTurn();
                break;
            case CombatActionType.ChangeEquipment:
                if (GetEquipmentItems().Count == 0)
                {
                    AddBattleLog("No equipment is available.");
                    break;
                }

                _gearMenu = new MenuController(GetEquipmentItems().Count);
                _combatMode = CombatMode.Gear;
                break;
            case CombatActionType.ViewCombatants:
                AddBattleLog("You study the battlefield.");
                break;
        }
    }

    private void UpdateTargetSelection(InputSnapshot input)
    {
        if (_combatState is null || _combatRandom is null)
        {
            return;
        }

        var interaction = _targetMenu.Handle(input, DateTimeOffset.UtcNow);

        if (interaction.Kind == MenuInteractionKind.Cancelled)
        {
            _combatMode = CombatMode.Action;
            return;
        }

        if (interaction.Kind != MenuInteractionKind.Confirmed)
        {
            return;
        }

        var targets = _combatState.LivingEnemies;

        if (targets.Count == 0)
        {
            _combatMode = CombatMode.Action;
            return;
        }

        var damage = _services.CombatService.Attack(_activeCombatant!, targets[Math.Clamp(interaction.SelectedIndex, 0, targets.Count - 1)], _combatRandom);
        AddBattleLog(FormatDamageLog(damage));
        EndPlayerTurn();
    }

    private void UpdateItemSelection(InputSnapshot input)
    {
        if (_combatState is null)
        {
            return;
        }

        var interaction = _itemMenu.Handle(input, DateTimeOffset.UtcNow);

        if (interaction.Kind == MenuInteractionKind.Cancelled)
        {
            _combatMode = CombatMode.Action;
            return;
        }

        if (interaction.Kind != MenuInteractionKind.Confirmed)
        {
            return;
        }

        var items = GetConsumableItems();

        if (items.Count == 0)
        {
            _combatMode = CombatMode.Action;
            return;
        }

        var item = items[Math.Clamp(interaction.SelectedIndex, 0, items.Count - 1)];
        var healing = _services.CombatService.UseConsumable(_combatState, item.Item.Id);
        AddBattleLog($"{healing.Target.Name} uses {healing.Consumable.Name} and heals {healing.AmountHealed}.");
        EndPlayerTurn();
    }

    private void UpdateGearSelection(InputSnapshot input)
    {
        if (_combatState is null)
        {
            return;
        }

        var interaction = _gearMenu.Handle(input, DateTimeOffset.UtcNow);

        if (interaction.Kind == MenuInteractionKind.Cancelled)
        {
            _combatMode = CombatMode.Action;
            return;
        }

        if (interaction.Kind != MenuInteractionKind.Confirmed)
        {
            return;
        }

        var gear = GetEquipmentItems();

        if (gear.Count == 0)
        {
            _combatMode = CombatMode.Action;
            return;
        }

        var item = gear[Math.Clamp(interaction.SelectedIndex, 0, gear.Count - 1)];
        _services.CombatService.ChangeEquipment(_combatState, _activeCombatant!, item);
        AddBattleLog($"{_activeCombatant!.Name} equips {item.Name}.");
        _combatMode = CombatMode.Action;
    }

    private void EndPlayerTurn()
    {
        _activeCombatant = null;
        _combatMode = CombatMode.None;

        if (_combatState?.IsComplete == true)
        {
            CompleteCombat();
        }
    }

    private void CompleteCombat()
    {
        if (_combatState is null || _combatRandom is null || _combatOutcome is not null)
        {
            return;
        }

        _combatOutcome = _combatState.IsVictory
            ? _services.CombatService.CompleteVictory(_combatState, _combatRandom)
            : _services.CombatService.CompleteDefeat(_combatState);

        if (_combatOutcome.OutcomeType == BattleOutcomeType.Victory)
        {
            AddBattleLog($"Victory! Gained {_combatOutcome.ExperienceAwarded} XP and {_combatOutcome.Reward.Currency} currency.");
        }
        else
        {
            AddBattleLog("Defeat. The party returns to safety.");
        }

        _autosave.MarkChanged();
    }

    private void FinishCombatScreen()
    {
        if (_session?.ActiveWorldSession is not null && _combatReturnsToWorld)
        {
            if (_combatOutcome?.OutcomeType == BattleOutcomeType.Victory)
            {
                _services.ExplorationService.ClearEnemyRoom(_session.ActiveWorldSession);

                if (_pendingWorldObject?.ObjectType == WorldObjectType.Enemy)
                {
                    _pendingWorldObject.MarkCleared();
                    _latestWorldEvent = MessageResult("The enemies are defeated.");
                }
                else
                {
                    _latestWorldEvent = _services.ExplorationService.EnterCurrentRoom(_session.Party, _session.ActiveWorldSession);
                }
            }

            _pendingEncounter = null;
            _pendingWorldObject = null;
            _autosave.MarkChanged();
            _screen = _combatOutcome?.OutcomeType == BattleOutcomeType.Victory
                ? ScreenKind.World
                : ScreenKind.Hub;
        }
        else
        {
            _screen = ScreenKind.Hub;
        }

        _combatState = null;
        _combatRandom = null;
        _combatOutcome = null;
        _activeCombatant = null;
        _combatMode = CombatMode.None;
    }

    private void UpdateDismissibleScreen(InputSnapshot input)
    {
        if (input.IsPressed(GameCommand.Confirm) ||
            input.IsPressed(GameCommand.Cancel) ||
            input.IsPressed(GameCommand.Menu))
        {
            _screen = _message?.ReturnTo ?? ScreenKind.Hub;
            _message = null;
        }
    }

    private GameViewModel BuildCurrentView()
    {
        return _screen switch
        {
            ScreenKind.MainMenu => BuildMainMenuView(),
            ScreenKind.LoadGame => BuildLoadGameView(),
            ScreenKind.Hub => BuildHubView(),
            ScreenKind.World => BuildWorldView(),
            ScreenKind.Combat => BuildCombatView(),
            ScreenKind.Party => BuildPartyView(),
            ScreenKind.Inventory => BuildInventoryView(),
            ScreenKind.Message => _message ?? new MessageViewModel("Message", "Continue.", ScreenKind.Hub),
            ScreenKind.Exit => new ExitViewModel(),
            _ => BuildMainMenuView()
        };
    }

    private MainMenuViewModel BuildMainMenuView()
    {
        return new MainMenuViewModel(BuildMenuOptions(Enum.GetValues<MainMenuAction>(), _mainMenu.SelectedIndex));
    }

    private LoadGameViewModel BuildLoadGameView()
    {
        var saves = _services.GamePersistence?.ListSaves() ?? [];
        var saveViews = saves
            .Select(save => new SaveSlotViewModel(save.Id, save.Name, save.LastPlayedAtUtc))
            .ToArray();
        var options = saveViews
            .Select((save, index) => new MenuOptionViewModel(save.Name, save.LastPlayedAtUtc.ToLocalTime().ToString("g"), index == _loadMenu.SelectedIndex))
            .ToArray();

        return new LoadGameViewModel(saveViews, options);
    }

    private HubViewModel BuildHubView()
    {
        return new HubViewModel(
            BuildPartySummary(_session?.Party),
            BuildMenuOptions(Enum.GetValues<CharacterHubAction>(), _hubMenu.SelectedIndex));
    }

    private WorldViewModel BuildWorldView()
    {
        var world = _session?.ActiveWorldSession;

        if (_session is null || world is null)
        {
            return new WorldViewModel(0, 0, 0, WorldPosition.Origin, [], BuildPartySummary(null), "None", "None", null, false, false);
        }

        var minimap = _services.MinimapService.CreateSnapshot(world);
        var room = world.CurrentRoom;
        var tileMap = GetTileMap(world);
        var playerPixel = CalculatePlayerPixelPosition(world, tileMap);
        var camera = tileMap is null ? null : BuildWorldCamera(world, tileMap);
        var objects = tileMap is null
            ? world.Objects.Select(BuildWorldObjectViewModel).ToArray()
            : world.Objects.Select(obj => BuildWorldObjectViewModel(obj, tileMap)).ToArray();

        return new WorldViewModel(
            world.Map.Seed,
            minimap.MinCoordinate,
            minimap.MaxCoordinate,
            world.CurrentPosition,
            minimap.Cells.Select(cell => new MapCellViewModel(cell.Position, cell.Symbol, cell.IsVisible)).ToArray(),
            BuildPartySummary(_session.Party),
            room.RoomType.GetDisplayName(),
            FormatRoomStatus(room),
            _latestWorldEvent?.Message,
            _pendingEncounter is not null,
            world.IsCompleted,
            BuildTileMapViewModel(tileMap),
            playerPixel.X,
            playerPixel.Y,
            _facingDirection,
            false,
            1f,
            _pendingEncounter?.Room.RoomType.GetDisplayName() ?? _pendingWorldObject?.ObjectType.GetDisplayName(),
            camera,
            objects,
            _pendingWorldObject is null ? null : CreateWorldObjectPrompt(_pendingWorldObject));
    }

    private CombatViewModel BuildCombatView()
    {
        if (_combatState is null)
        {
            return new CombatViewModel(0, [], [], null, null, [], [], [], [], _battleLog.ToArray(), null, null);
        }

        return new CombatViewModel(
            _combatState.Seed,
            _combatState.Enemies.Select(BuildCombatant).ToArray(),
            _combatState.PartyCombatants.Select(BuildCombatant).ToArray(),
            _activeCombatant?.Name,
            _activeCombatant?.Team,
            _combatMode == CombatMode.Action ? BuildMenuOptions(Enum.GetValues<CombatActionType>(), _combatActionMenu.SelectedIndex) : [],
            _combatMode == CombatMode.Target ? BuildCombatantOptions(_combatState.LivingEnemies, _targetMenu.SelectedIndex) : [],
            _combatMode == CombatMode.Item ? BuildInventoryOptions(GetConsumableItems(), _itemMenu.SelectedIndex) : [],
            _combatMode == CombatMode.Gear ? BuildGearOptions(GetEquipmentItems(), _gearMenu.SelectedIndex) : [],
            _battleLog.ToArray(),
            _combatOutcome?.OutcomeType.GetDisplayName(),
            _combatOutcome is null ? null : FormatBattleOutcome(_combatOutcome));
    }

    private TileMapViewModel? BuildTileMapViewModel(TileMapDefinition? tileMap)
    {
        if (tileMap is null)
        {
            return null;
        }

        var layers = tileMap.Layers
            .Select(layer => new TileLayerViewModel(
                layer.Name,
                layer.Width,
                layer.Height,
                layer.Visible,
                Enumerable.Range(0, layer.Height)
                    .SelectMany(y => Enumerable.Range(0, layer.Width)
                        .Select(x =>
                        {
                            var position = new WorldPosition(x, y);
                            var globalId = layer.GetTile(x, y);
                            var tile = tileMap.FindTile(globalId);

                            return new TileViewModel(
                                position,
                                globalId,
                                tile?.Type ?? string.Empty,
                                tile?.IsBlocking ?? false);
                        }))
                    .ToArray()))
            .ToArray();
        var firstTileset = tileMap.Tilesets.FirstOrDefault();

        return new TileMapViewModel(
            tileMap.Name,
            tileMap.Width,
            tileMap.Height,
            tileMap.TileWidth,
            tileMap.TileHeight,
            layers,
            tileMap.Objects.Select(obj => new TileObjectViewModel(obj.Name, obj.Type, obj.TilePosition)).ToArray(),
            firstTileset?.Columns ?? 0,
            firstTileset?.TileCount ?? 0);
    }

    private TileMapDefinition? GetTileMap(WorldSession world)
    {
        return _tileMapProvider?.GetTileMap(world) ?? _fallbackTileMap;
    }

    private (float X, float Y) CalculatePlayerPixelPosition(WorldSession world, TileMapDefinition? tileMap)
    {
        if (tileMap is null)
        {
            return (world.CurrentPosition.X * 48f, -world.CurrentPosition.Y * 48f);
        }

        var tile = ToTilePosition(world.CurrentPosition, tileMap);
        var x = tile.X * tileMap.TileWidth;
        var y = tile.Y * tileMap.TileHeight;

        return (x, y);
    }

    private WorldPosition ToTilePosition(WorldPosition worldPosition, TileMapDefinition tileMap)
    {
        return new WorldPosition(
            tileMap.SpawnTile.X + worldPosition.X,
            tileMap.SpawnTile.Y - worldPosition.Y);
    }

    private WorldCameraViewModel BuildWorldCamera(WorldSession world, TileMapDefinition tileMap)
    {
        var playerTile = ToTilePosition(world.CurrentPosition, tileMap);
        var maxMinX = Math.Max(0, tileMap.Width - WorldCameraColumns);
        var maxMinY = Math.Max(0, tileMap.Height - WorldCameraRows);
        var minX = Math.Clamp(playerTile.X - (WorldCameraColumns / 2), 0, maxMinX);
        var minY = Math.Clamp(playerTile.Y - (WorldCameraRows / 2), 0, maxMinY);
        var visibleColumns = Math.Min(WorldCameraColumns, tileMap.Width);
        var visibleRows = Math.Min(WorldCameraRows, tileMap.Height);

        return new WorldCameraViewModel(
            minX,
            minY,
            minX + visibleColumns - 1,
            minY + visibleRows - 1,
            visibleColumns,
            visibleRows,
            -minX * tileMap.TileWidth,
            -minY * tileMap.TileHeight);
    }

    private WorldObjectViewModel BuildWorldObjectViewModel(WorldObject worldObject)
    {
        return new WorldObjectViewModel(
            worldObject.Id,
            worldObject.ObjectType.GetDisplayName(),
            worldObject.Position,
            worldObject.State.GetDisplayName(),
            worldObject.IsBlocking,
            worldObject.IsActive);
    }

    private WorldObjectViewModel BuildWorldObjectViewModel(WorldObject worldObject, TileMapDefinition tileMap)
    {
        var tilePosition = ToTilePosition(worldObject.Position, tileMap);

        return new WorldObjectViewModel(
            worldObject.Id,
            worldObject.ObjectType.GetDisplayName(),
            tilePosition,
            worldObject.State.GetDisplayName(),
            worldObject.IsBlocking,
            worldObject.IsActive);
    }

    private PartyViewModel BuildPartyView()
    {
        var party = _session?.Party;
        var members = party is null
            ? []
            : party.Roster.Select(member => BuildPartyMember(member, party.GetLocation(member.Id).GetDisplayName())).ToArray();

        return new PartyViewModel(BuildPartySummary(party), members, "Party");
    }

    private InventoryViewModel BuildInventoryView()
    {
        var party = _session?.Party;
        var items = party?.Inventory.Items
            .OrderBy(stack => stack.Item.Kind)
            .ThenBy(stack => stack.Item.Name)
            .Select(stack => new InventoryStackViewModel(stack.Item.Id, stack.Item.Name, stack.Item.Kind.GetDisplayName(), stack.Quantity))
            .ToArray() ?? [];

        return new InventoryViewModel(BuildPartySummary(party), party?.Inventory.Currency ?? 0, items);
    }

    private static IReadOnlyList<MenuOptionViewModel> BuildMenuOptions<T>(IReadOnlyList<T> values, int selectedIndex)
        where T : Enum
    {
        return values
            .Select((value, index) => new MenuOptionViewModel(value.GetDisplayName(), null, index == selectedIndex))
            .ToArray();
    }

    private static IReadOnlyList<MenuOptionViewModel> BuildCombatantOptions(IReadOnlyList<Combatant> combatants, int selectedIndex)
    {
        return combatants
            .Select((combatant, index) => new MenuOptionViewModel(
                $"{combatant.Name} {combatant.CurrentHealth}/{combatant.MaxHealth} HP",
                null,
                index == selectedIndex))
            .ToArray();
    }

    private static IReadOnlyList<MenuOptionViewModel> BuildInventoryOptions(IReadOnlyList<InventoryStack> stacks, int selectedIndex)
    {
        return stacks
            .Select((stack, index) => new MenuOptionViewModel($"{stack.Item.Name} x{stack.Quantity}", null, index == selectedIndex))
            .ToArray();
    }

    private static IReadOnlyList<MenuOptionViewModel> BuildGearOptions(IReadOnlyList<IEquipmentItem> gear, int selectedIndex)
    {
        return gear
            .Select((item, index) => new MenuOptionViewModel($"{item.Name} ({item.Slot.GetDisplayName()})", null, index == selectedIndex))
            .ToArray();
    }

    private static PartySummaryViewModel BuildPartySummary(Party? party)
    {
        if (party is null)
        {
            return new PartySummaryViewModel("No Party", 0, Party.MaxActiveMembers, 0, 0, []);
        }

        return new PartySummaryViewModel(
            party.Leader.Name,
            party.ActiveMembers.Count,
            Party.MaxActiveMembers,
            party.Steps,
            party.Inventory.Currency,
            party.ActiveMembers.Select(member => BuildPartyMember(member, PartyMemberLocation.Active.GetDisplayName())).ToArray());
    }

    private static PartyMemberViewModel BuildPartyMember(Actor member, string location)
    {
        return new PartyMemberViewModel(
            member.Id,
            member.Name,
            member.CharacterClass.GetDisplayName(),
            member.Stats[StatType.Health].CurrentValue,
            member.Stats[StatType.Health].MaxValue,
            member.Equipment.EquippedWeapon.Name,
            location);
    }

    private CombatantViewModel BuildCombatant(Combatant combatant)
    {
        return new CombatantViewModel(
            combatant.Id,
            combatant.Name,
            combatant.Team,
            combatant.CurrentHealth,
            combatant.MaxHealth,
            combatant.IsAlive,
            combatant.IsDefending,
            _activeCombatant?.Id == combatant.Id);
    }

    private IReadOnlyList<InventoryStack> GetConsumableItems()
    {
        return _session?.Party.Inventory.Items
            .Where(stack => stack.Item is ConsumableDefinition)
            .ToArray() ?? [];
    }

    private IReadOnlyList<IEquipmentItem> GetEquipmentItems()
    {
        return _session?.Party.Inventory.Items
            .Where(stack => stack.Item is IEquipmentItem)
            .Select(stack => (IEquipmentItem)stack.Item)
            .ToArray() ?? [];
    }

    private void SaveCurrentSession(bool force)
    {
        if (_session is null)
        {
            return;
        }

        if (_screen == ScreenKind.World && _session.ActiveWorldSession is not null)
        {
            _session = _session with { ActiveWorldSession = _session.ActiveWorldSession };
        }

        _services.GamePersistence?.Save(_session);
    }

    private void AddBattleLog(string message)
    {
        _battleLog.Add(message);

        if (_battleLog.Count > MaxBattleLogEntries)
        {
            _battleLog.RemoveAt(0);
        }
    }

    private void ShowMessage(string title, string message, ScreenKind returnTo)
    {
        _message = new MessageViewModel(title, message, returnTo);
        _screen = ScreenKind.Message;
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

    private bool TryGetWorldStepDirection(InputSnapshot input, out Direction direction)
    {
        var commands = new[]
        {
            GameCommand.MoveUp,
            GameCommand.MoveDown,
            GameCommand.MoveLeft,
            GameCommand.MoveRight
        };

        foreach (var command in commands)
        {
            if (input.IsPressed(command) && TryGetDirection(command, out direction))
            {
                return true;
            }
        }

        if (!_worldDirectionHeldLastFrame)
        {
            foreach (var command in commands)
            {
                if (input.IsHeld(command) && TryGetDirection(command, out direction))
                {
                    return true;
                }
            }
        }

        direction = default;
        return false;
    }

    private static bool HasWorldDirectionHeld(InputSnapshot input)
    {
        return input.IsHeld(GameCommand.MoveUp) ||
            input.IsHeld(GameCommand.MoveDown) ||
            input.IsHeld(GameCommand.MoveLeft) ||
            input.IsHeld(GameCommand.MoveRight);
    }

    private static string FormatDamageLog(DamageResult result)
    {
        var critical = result.IsCritical ? " Critical!" : string.Empty;
        var defeated = result.TargetDefeated ? " Defeated!" : string.Empty;

        return $"{result.Attacker.Name} hits {result.Target.Name} for {result.Damage}.{critical}{defeated}";
    }

    private static string FormatBattleOutcome(BattleOutcome outcome)
    {
        return outcome.OutcomeType == BattleOutcomeType.Defeat
            ? "The party was defeated and returns to safety."
            : $"Rewards: {outcome.Reward.Currency} currency, {outcome.ExperienceAwarded} XP.";
    }

    private static string FormatRoomStatus(WorldRoom room)
    {
        return room.RoomType switch
        {
            RoomType.Enemy => room.IsCleared ? "Cleared" : "Hostile",
            RoomType.Treasure => room.IsLooted ? "Looted" : "Unlooted",
            RoomType.Exit => "Exit",
            RoomType.Start => "Safe",
            _ => room.IsVisited ? "Visited" : "Unvisited"
        };
    }

    private enum CombatMode
    {
        None,
        Action,
        Target,
        Item,
        Gear
    }
}
