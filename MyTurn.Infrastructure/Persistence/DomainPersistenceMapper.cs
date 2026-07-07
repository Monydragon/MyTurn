using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Infrastructure.Data.Entities;

namespace MyTurn.Infrastructure.Persistence;

internal sealed class DomainPersistenceMapper
{
    private readonly IItemDefinitionRegistry _items;
    private readonly IWeaponDefinitionRegistry _weapons;
    private readonly ISkillDefinitionRegistry _skillDefinitions;
    private readonly IStatDefinitionRegistry _statDefinitions;

    public DomainPersistenceMapper(
        IItemDefinitionRegistry items,
        IWeaponDefinitionRegistry weapons,
        ISkillDefinitionRegistry skillDefinitions,
        IStatDefinitionRegistry statDefinitions)
    {
        _items = items;
        _weapons = weapons;
        _skillDefinitions = skillDefinitions;
        _statDefinitions = statDefinitions;
    }

    public IReadOnlyList<PartyMemberEntity> CreatePartyMemberEntities(Guid saveSlotId, Party party)
    {
        ArgumentNullException.ThrowIfNull(party);

        return party.ActiveMembers
            .Select((member, index) => CreatePartyMemberEntity(saveSlotId, member, PartyMemberLocation.Active, index))
            .Concat(party.ReserveMembers.Select(member => CreatePartyMemberEntity(saveSlotId, member, PartyMemberLocation.Reserve, null)))
            .ToArray();
    }

    public PartyMemberEntity CreatePartyMemberEntity(
        Guid saveSlotId,
        Actor actor,
        PartyMemberLocation location,
        int? activeOrder)
    {
        var entity = new PartyMemberEntity
        {
            Id = actor.Id,
            SaveSlotId = saveSlotId
        };

        UpdatePartyMemberEntity(entity, actor, location, activeOrder);

        return entity;
    }

    public void UpdatePartyMemberEntity(
        PartyMemberEntity entity,
        Actor actor,
        PartyMemberLocation location,
        int? activeOrder)
    {
        entity.Id = actor.Id;
        entity.Name = actor.Name;
        entity.Age = actor.Age;
        entity.Gender = actor.Gender.ToString();
        entity.Species = actor.Species.ToString();
        entity.CharacterClass = actor.CharacterClass.ToString();
        entity.Steps = actor.Steps;
        entity.Location = location.ToString();
        entity.ActiveOrder = activeOrder;

        entity.Stats.Clear();
        entity.Stats.AddRange(actor.Stats.Select(stat => new PartyMemberStatEntity
        {
            PartyMemberId = actor.Id,
            StatType = stat.StatType.ToString(),
            BaseValue = stat.BaseValue,
            MaxValue = stat.MaxValue
        }));

        entity.Skills.Clear();
        entity.Skills.AddRange(actor.Skills.Select(skill => new PartyMemberSkillEntity
        {
            PartyMemberId = actor.Id,
            SkillType = skill.SkillType.ToString(),
            Name = skill.Leveling.Name,
            CurrentLevel = skill.Leveling.CurrentLevel,
            Experience = skill.Leveling.Experience,
            MaxLevel = skill.Leveling.MaxLevel
        }));

        entity.Equipment.Clear();
        entity.Equipment.AddRange(actor.Equipment.EquippedItems.Select(item => new PartyMemberEquipmentEntity
        {
            PartyMemberId = actor.Id,
            Slot = item.Key.ToString(),
            ItemId = item.Value.Id
        }));
    }

    public IReadOnlyList<PartyInventoryStackEntity> CreateInventoryStackEntities(Guid saveSlotId, Inventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);

        return inventory.Items.Select(stack => new PartyInventoryStackEntity
        {
            SaveSlotId = saveSlotId,
            ItemId = stack.Item.Id,
            Quantity = stack.Quantity
        }).ToArray();
    }

    public Party ToParty(SaveSlotEntity entity)
    {
        var inventory = new Inventory();

        if (entity.Currency > 0)
        {
            inventory.AddCurrency(entity.Currency);
        }

        foreach (var stack in entity.InventoryStacks)
        {
            inventory.Add(_items.Get(stack.ItemId), stack.Quantity);
        }

        var activeMembers = entity.PartyMembers
            .Where(member => ParseLocation(member.Location) == PartyMemberLocation.Active)
            .OrderBy(member => member.ActiveOrder ?? int.MaxValue)
            .ThenBy(member => member.Name)
            .Select(member => ToActor(member, inventory))
            .ToArray();
        var reserveMembers = entity.PartyMembers
            .Where(member => ParseLocation(member.Location) == PartyMemberLocation.Reserve)
            .OrderBy(member => member.Name)
            .Select(member => ToActor(member, inventory))
            .ToArray();

        return new Party(activeMembers, reserveMembers, inventory, entity.Steps, entity.Id);
    }

    public Actor ToActor(PartyMemberEntity entity, Inventory inventory)
    {
        var characterClass = Enum.Parse<CharacterClass>(entity.CharacterClass);
        var equipmentItems = entity.Equipment
            .Select(equipment => (Slot: Enum.Parse<EquipmentSlot>(equipment.Slot), Item: _items.Get(equipment.ItemId)))
            .ToArray();
        var weapon = equipmentItems
            .Select(equipment => equipment.Item)
            .OfType<IWeapon>()
            .FirstOrDefault()
            ?? _weapons.Get(GetStartingWeaponType(characterClass));
        var equipmentLoadout = new EquipmentLoadout(weapon);

        foreach (var equipment in equipmentItems)
        {
            if (equipment.Item is IEquipmentItem equipmentItem && equipment.Slot != EquipmentSlot.Weapon)
            {
                equipmentLoadout.Equip(equipmentItem);
            }
        }

        var actor = new Actor(
            entity.Name,
            entity.Age,
            Enum.Parse<Gender>(entity.Gender),
            Enum.Parse<Species>(entity.Species),
            characterClass,
            CreateSkillSet(entity),
            CreateStatSet(entity),
            equipmentLoadout,
            inventory,
            entity.Id);

        if (entity.Steps > 0)
        {
            actor.AddSteps(entity.Steps);
        }

        ApplyEquipmentModifiers(actor);

        return actor;
    }

    public WorldSessionEntity CreateWorldSessionEntity(Guid saveSlotId, WorldSession session)
    {
        var entity = new WorldSessionEntity
        {
            Id = session.Id,
            SaveSlotId = saveSlotId,
            CreatedAtUtc = DateTime.UtcNow
        };

        UpdateWorldSessionEntity(entity, session);

        return entity;
    }

    public void UpdateWorldSessionEntity(WorldSessionEntity entity, WorldSession session)
    {
        entity.Id = session.Id;
        entity.Seed = session.Map.Seed;
        entity.LayoutId = session.LayoutId;
        entity.ProfileId = session.ProfileId;
        entity.LayoutSource = session.LayoutSource;
        entity.MinCoordinate = session.Map.MinCoordinate;
        entity.MaxCoordinate = session.Map.MaxCoordinate;
        entity.CurrentX = session.CurrentPosition.X;
        entity.CurrentY = session.CurrentPosition.Y;
        entity.IsCompleted = session.IsCompleted;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        if (entity.CreatedAtUtc == default)
        {
            entity.CreatedAtUtc = entity.UpdatedAtUtc;
        }

        entity.Rooms.Clear();
        entity.Rooms.AddRange(session.Map.Rooms.Values.Select(room => new WorldRoomEntity
        {
            WorldSessionId = session.Id,
            X = room.Position.X,
            Y = room.Position.Y,
            RoomType = room.RoomType.ToString(),
            EncounterSeed = room.EncounterSeed,
            IsVisited = room.IsVisited,
            IsCleared = room.IsCleared,
            IsLooted = room.IsLooted
        }));
        entity.Objects.Clear();
        entity.Objects.AddRange(session.Objects.Select(worldObject => new WorldObjectEntity
        {
            WorldSessionId = session.Id,
            ObjectId = worldObject.Id,
            ObjectType = worldObject.ObjectType.ToString(),
            X = worldObject.Position.X,
            Y = worldObject.Position.Y,
            IsBlocking = worldObject.IsBlocking,
            State = worldObject.State.ToString(),
            EncounterSeed = worldObject.EncounterSeed,
            PayloadJson = worldObject.PayloadJson
        }));
    }

    public WorldSession ToWorldSession(WorldSessionEntity entity)
    {
        var rooms = entity.Rooms.Select(room => new WorldRoom(
            new WorldPosition(room.X, room.Y),
            Enum.Parse<RoomType>(room.RoomType),
            room.EncounterSeed,
            room.IsVisited,
            room.IsCleared,
            room.IsLooted));
        var objects = entity.Objects.Select(worldObject => new WorldObject(
            worldObject.ObjectId,
            Enum.Parse<WorldObjectType>(worldObject.ObjectType),
            new WorldPosition(worldObject.X, worldObject.Y),
            worldObject.IsBlocking,
            Enum.Parse<WorldObjectState>(worldObject.State),
            worldObject.EncounterSeed,
            worldObject.PayloadJson));
        var map = new WorldMap(entity.Seed, entity.MinCoordinate, entity.MaxCoordinate, rooms);

        return new WorldSession(
            map,
            new WorldPosition(entity.CurrentX, entity.CurrentY),
            entity.IsCompleted,
            entity.Id,
            entity.LayoutId,
            entity.ProfileId,
            entity.LayoutSource,
            objects);
    }

    private StatSet CreateStatSet(PartyMemberEntity entity)
    {
        var persisted = entity.Stats.ToDictionary(stat => Enum.Parse<StatType>(stat.StatType));
        var stats = _statDefinitions.Definitions.Select(definition =>
        {
            if (persisted.TryGetValue(definition.StatType, out var stat))
            {
                return new Stat(definition.StatType, stat.BaseValue, stat.MaxValue);
            }

            return new Stat(definition.StatType, definition.BaseValue, definition.MaxValue);
        });

        return new StatSet(stats);
    }

    private SkillSet CreateSkillSet(PartyMemberEntity entity)
    {
        var persisted = entity.Skills.ToDictionary(skill => Enum.Parse<SkillType>(skill.SkillType));
        var skills = _skillDefinitions.Definitions.Select(definition =>
        {
            if (persisted.TryGetValue(definition.SkillType, out var skill))
            {
                return new Skill(
                    definition.SkillType,
                    new LevelContainer(skill.Name, skill.CurrentLevel, skill.Experience, skill.MaxLevel));
            }

            return new Skill(
                definition.SkillType,
                new LevelContainer(
                    definition.Name,
                    definition.StartingLevel,
                    definition.StartingExperience,
                    definition.MaxLevel));
        });

        return new SkillSet(skills);
    }

    private static PartyMemberLocation ParseLocation(string location)
    {
        return Enum.TryParse<PartyMemberLocation>(location, out var parsed)
            ? parsed
            : PartyMemberLocation.Reserve;
    }

    private static void ApplyEquipmentModifiers(Actor actor)
    {
        foreach (var item in actor.Equipment.EquippedItems.Values)
        {
            var sourceId = EquipmentService.GetEquipmentSourceId(item.Slot);
            actor.Stats.ApplyModifiers(item.StatModifiers.Select(modifier =>
                new StatModifier(modifier.StatType, modifier.Value, sourceId)));
        }
    }

    private static WeaponType GetStartingWeaponType(CharacterClass characterClass)
    {
        return characterClass switch
        {
            CharacterClass.Warrior => WeaponType.Melee,
            CharacterClass.Archer => WeaponType.Ranged,
            CharacterClass.Mage => WeaponType.Magic,
            _ => throw new ArgumentOutOfRangeException(nameof(characterClass), characterClass, "Unsupported character class.")
        };
    }
}
