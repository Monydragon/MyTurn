namespace MyTurn.Infrastructure.Data.Entities;

public sealed class ItemDefinitionEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public bool IsStackable { get; set; }
    public string? WeaponType { get; set; }
    public int? MinDamage { get; set; }
    public int? MaxDamage { get; set; }
    public string? EquipmentSlot { get; set; }
    public int? HealingAmount { get; set; }
    public List<ItemStatModifierEntity> StatModifiers { get; set; } = [];
}

public sealed class ItemStatModifierEntity
{
    public int Id { get; set; }
    public string ItemDefinitionId { get; set; } = string.Empty;
    public string StatType { get; set; } = string.Empty;
    public int Value { get; set; }
    public ItemDefinitionEntity? ItemDefinition { get; set; }
}

public sealed class EnemyDefinitionEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WeaponItemId { get; set; } = string.Empty;
    public int ExperienceReward { get; set; }
    public ItemDefinitionEntity? WeaponItem { get; set; }
    public EnemySpawnWeightEntity? SpawnWeight { get; set; }
    public List<EnemyStatEntity> Stats { get; set; } = [];
    public List<EnemyActionEntity> Actions { get; set; } = [];
    public List<EnemyLootDropEntity> LootDrops { get; set; } = [];
}

public sealed class EnemyStatEntity
{
    public int Id { get; set; }
    public string EnemyDefinitionId { get; set; } = string.Empty;
    public string StatType { get; set; } = string.Empty;
    public int Value { get; set; }
    public EnemyDefinitionEntity? EnemyDefinition { get; set; }
}

public sealed class EnemyActionEntity
{
    public int Id { get; set; }
    public string EnemyDefinitionId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public int Weight { get; set; }
    public EnemyDefinitionEntity? EnemyDefinition { get; set; }
}

public sealed class EnemyLootDropEntity
{
    public int Id { get; set; }
    public string EnemyDefinitionId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public long MinQuantity { get; set; }
    public long MaxQuantity { get; set; }
    public int Weight { get; set; }
    public EnemyDefinitionEntity? EnemyDefinition { get; set; }
}

public sealed class EnemySpawnWeightEntity
{
    public int Id { get; set; }
    public string EnemyDefinitionId { get; set; } = string.Empty;
    public int Weight { get; set; }
    public EnemyDefinitionEntity? EnemyDefinition { get; set; }
}

public sealed class SaveSlotEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastPlayedAtUtc { get; set; }
    public PlayerEntity? Player { get; set; }
    public List<WorldSessionEntity> WorldSessions { get; set; } = [];
}

public sealed class PlayerEntity
{
    public Guid Id { get; set; }
    public Guid SaveSlotId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string CharacterClass { get; set; } = string.Empty;
    public long Steps { get; set; }
    public long Currency { get; set; }
    public SaveSlotEntity? SaveSlot { get; set; }
    public List<PlayerStatEntity> Stats { get; set; } = [];
    public List<PlayerSkillEntity> Skills { get; set; } = [];
    public List<PlayerInventoryStackEntity> InventoryStacks { get; set; } = [];
    public List<PlayerEquipmentEntity> Equipment { get; set; } = [];
}

public sealed class PlayerStatEntity
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public string StatType { get; set; } = string.Empty;
    public int BaseValue { get; set; }
    public int MaxValue { get; set; }
    public PlayerEntity? Player { get; set; }
}

public sealed class PlayerSkillEntity
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public string SkillType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CurrentLevel { get; set; }
    public int Experience { get; set; }
    public int MaxLevel { get; set; }
    public PlayerEntity? Player { get; set; }
}

public sealed class PlayerInventoryStackEntity
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public PlayerEntity? Player { get; set; }
    public ItemDefinitionEntity? ItemDefinition { get; set; }
}

public sealed class PlayerEquipmentEntity
{
    public int Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Slot { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public PlayerEntity? Player { get; set; }
    public ItemDefinitionEntity? ItemDefinition { get; set; }
}

public sealed class WorldSessionEntity
{
    public Guid Id { get; set; }
    public Guid SaveSlotId { get; set; }
    public int Seed { get; set; }
    public int MinCoordinate { get; set; }
    public int MaxCoordinate { get; set; }
    public int CurrentX { get; set; }
    public int CurrentY { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public SaveSlotEntity? SaveSlot { get; set; }
    public List<WorldRoomEntity> Rooms { get; set; } = [];
}

public sealed class WorldRoomEntity
{
    public int Id { get; set; }
    public Guid WorldSessionId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string RoomType { get; set; } = string.Empty;
    public int? EncounterSeed { get; set; }
    public bool IsVisited { get; set; }
    public bool IsCleared { get; set; }
    public bool IsLooted { get; set; }
    public WorldSessionEntity? WorldSession { get; set; }
}
