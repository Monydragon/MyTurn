namespace MyTurn.Infrastructure.Data.Entities;

public sealed class ItemDefinitionEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public bool IsStackable { get; set; }
    public int Tier { get; set; } = 1;
    public int SuggestedLevel { get; set; } = 1;
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
    public int ThreatRating { get; set; } = 1;
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
    public long Steps { get; set; }
    public long Currency { get; set; }
    public List<PartyMemberEntity> PartyMembers { get; set; } = [];
    public List<PartyInventoryStackEntity> InventoryStacks { get; set; } = [];
    public List<WorldSessionEntity> WorldSessions { get; set; } = [];
}

public sealed class PartyMemberEntity
{
    public Guid Id { get; set; }
    public Guid SaveSlotId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string CharacterClass { get; set; } = string.Empty;
    public long Steps { get; set; }
    public string Location { get; set; } = string.Empty;
    public int? ActiveOrder { get; set; }
    public SaveSlotEntity? SaveSlot { get; set; }
    public List<PartyMemberStatEntity> Stats { get; set; } = [];
    public List<PartyMemberSkillEntity> Skills { get; set; } = [];
    public List<PartyMemberEquipmentEntity> Equipment { get; set; } = [];
}

public sealed class PartyMemberStatEntity
{
    public int Id { get; set; }
    public Guid PartyMemberId { get; set; }
    public string StatType { get; set; } = string.Empty;
    public int BaseValue { get; set; }
    public int MaxValue { get; set; }
    public PartyMemberEntity? PartyMember { get; set; }
}

public sealed class PartyMemberSkillEntity
{
    public int Id { get; set; }
    public Guid PartyMemberId { get; set; }
    public string SkillType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CurrentLevel { get; set; }
    public int Experience { get; set; }
    public int MaxLevel { get; set; }
    public PartyMemberEntity? PartyMember { get; set; }
}

public sealed class PartyInventoryStackEntity
{
    public int Id { get; set; }
    public Guid SaveSlotId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public SaveSlotEntity? SaveSlot { get; set; }
    public ItemDefinitionEntity? ItemDefinition { get; set; }
}

public sealed class PartyMemberEquipmentEntity
{
    public int Id { get; set; }
    public Guid PartyMemberId { get; set; }
    public string Slot { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public PartyMemberEntity? PartyMember { get; set; }
    public ItemDefinitionEntity? ItemDefinition { get; set; }
}

public sealed class WorldSessionEntity
{
    public Guid Id { get; set; }
    public Guid SaveSlotId { get; set; }
    public int Seed { get; set; }
    public string? LayoutId { get; set; }
    public string? ProfileId { get; set; }
    public string? LayoutSource { get; set; }
    public int MinCoordinate { get; set; }
    public int MaxCoordinate { get; set; }
    public int CurrentX { get; set; }
    public int CurrentY { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public SaveSlotEntity? SaveSlot { get; set; }
    public List<WorldRoomEntity> Rooms { get; set; } = [];
    public List<WorldObjectEntity> Objects { get; set; } = [];
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

public sealed class WorldObjectEntity
{
    public int Id { get; set; }
    public Guid WorldSessionId { get; set; }
    public string ObjectId { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsBlocking { get; set; }
    public string State { get; set; } = string.Empty;
    public int? EncounterSeed { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public WorldSessionEntity? WorldSession { get; set; }
}
