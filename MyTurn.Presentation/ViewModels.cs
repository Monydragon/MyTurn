using MyTurn.Domain;

namespace MyTurn.Presentation;

public enum ScreenKind
{
    MainMenu,
    LoadGame,
    Hub,
    World,
    Combat,
    Party,
    Inventory,
    Message,
    Exit
}

public abstract record GameViewModel(ScreenKind ScreenKind);

public sealed record MenuOptionViewModel(
    string Label,
    string? Detail = null,
    bool IsSelected = false);

public sealed record MainMenuViewModel(IReadOnlyList<MenuOptionViewModel> Options)
    : GameViewModel(ScreenKind.MainMenu);

public sealed record LoadGameViewModel(
    IReadOnlyList<SaveSlotViewModel> Saves,
    IReadOnlyList<MenuOptionViewModel> Options)
    : GameViewModel(ScreenKind.LoadGame);

public sealed record HubViewModel(
    PartySummaryViewModel Party,
    IReadOnlyList<MenuOptionViewModel> Options)
    : GameViewModel(ScreenKind.Hub);

public sealed record WorldViewModel(
    int Seed,
    int MinCoordinate,
    int MaxCoordinate,
    WorldPosition CurrentPosition,
    IReadOnlyList<MapCellViewModel> Cells,
    PartySummaryViewModel Party,
    string RoomType,
    string RoomStatus,
    string? LatestEvent,
    bool HasPendingEncounter,
    bool IsCompleted,
    TileMapViewModel? TileMap = null,
    float PlayerPixelX = 0,
    float PlayerPixelY = 0,
    Direction FacingDirection = Direction.South,
    bool IsMoving = false,
    float MovementProgress = 0,
    string? PendingEventType = null,
    WorldCameraViewModel? Camera = null,
    IReadOnlyList<WorldObjectViewModel>? Objects = null,
    WorldInteractionPromptViewModel? InteractionPrompt = null)
    : GameViewModel(ScreenKind.World);

public sealed record CombatViewModel(
    int Seed,
    IReadOnlyList<CombatantViewModel> Enemies,
    IReadOnlyList<CombatantViewModel> Party,
    string? ActiveCombatantName,
    CombatTeam? ActiveTeam,
    IReadOnlyList<MenuOptionViewModel> CommandOptions,
    IReadOnlyList<MenuOptionViewModel> TargetOptions,
    IReadOnlyList<MenuOptionViewModel> ItemOptions,
    IReadOnlyList<MenuOptionViewModel> GearOptions,
    IReadOnlyList<string> BattleLog,
    string? OutcomeTitle,
    string? OutcomeText)
    : GameViewModel(ScreenKind.Combat);

public sealed record PartyViewModel(
    PartySummaryViewModel Party,
    IReadOnlyList<PartyMemberViewModel> Members,
    string Title)
    : GameViewModel(ScreenKind.Party);

public sealed record InventoryViewModel(
    PartySummaryViewModel Party,
    long Currency,
    IReadOnlyList<InventoryStackViewModel> Items)
    : GameViewModel(ScreenKind.Inventory);

public sealed record MessageViewModel(
    string Title,
    string Message,
    ScreenKind ReturnTo)
    : GameViewModel(ScreenKind.Message);

public sealed record ExitViewModel()
    : GameViewModel(ScreenKind.Exit);

public sealed record SaveSlotViewModel(
    Guid Id,
    string Name,
    DateTime LastPlayedAtUtc);

public sealed record PartySummaryViewModel(
    string LeaderName,
    int ActiveCount,
    int MaxActiveCount,
    long Steps,
    long Currency,
    IReadOnlyList<PartyMemberViewModel> ActiveMembers);

public sealed record PartyMemberViewModel(
    Guid Id,
    string Name,
    string ClassName,
    int CurrentHealth,
    int MaxHealth,
    string WeaponName,
    string Location);

public sealed record MapCellViewModel(
    WorldPosition Position,
    char Symbol,
    bool IsVisible);

public sealed record CombatantViewModel(
    Guid Id,
    string Name,
    CombatTeam Team,
    int CurrentHealth,
    int MaxHealth,
    bool IsAlive,
    bool IsDefending,
    bool IsActive);

public sealed record InventoryStackViewModel(
    string ItemId,
    string Name,
    string Kind,
    int Quantity);

public sealed record WorldCameraViewModel(
    int MinTileX,
    int MinTileY,
    int MaxTileX,
    int MaxTileY,
    int VisibleColumns,
    int VisibleRows,
    float OriginPixelX,
    float OriginPixelY);

public sealed record WorldObjectViewModel(
    string Id,
    string Type,
    WorldPosition Position,
    string State,
    bool IsBlocking,
    bool IsActive);

public sealed record WorldInteractionPromptViewModel(
    string ObjectId,
    string ObjectType,
    string Title,
    string Message,
    bool RequiresConfirm);
