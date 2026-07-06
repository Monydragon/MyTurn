using Microsoft.EntityFrameworkCore;
using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Infrastructure.Data;

namespace MyTurn.Infrastructure.Persistence;

public sealed class SqliteGamePersistenceService : IGamePersistenceService
{
    private readonly DbContextOptions<MyTurnDbContext> _options;
    private readonly DomainPersistenceMapper _mapper;

    public SqliteGamePersistenceService(
        DbContextOptions<MyTurnDbContext> options,
        IItemDefinitionRegistry items,
        IWeaponDefinitionRegistry weapons,
        ISkillDefinitionRegistry skillDefinitions,
        IStatDefinitionRegistry statDefinitions)
    {
        _options = options;
        _mapper = new DomainPersistenceMapper(items, weapons, skillDefinitions, statDefinitions);
    }

    public IReadOnlyList<SaveSlotSummary> ListSaves()
    {
        using var db = CreateContext();

        return db.SaveSlots
            .AsNoTracking()
            .OrderByDescending(slot => slot.LastPlayedAtUtc)
            .Select(slot => new SaveSlotSummary(slot.Id, slot.Name, slot.CreatedAtUtc, slot.LastPlayedAtUtc))
            .ToArray();
    }

    public GameSession CreateSave(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return CreateSave(new Party([actor], inventory: actor.Inventory, steps: actor.Steps));
    }

    public GameSession CreateSave(Party party)
    {
        ArgumentNullException.ThrowIfNull(party);

        using var db = CreateContext();
        var now = DateTime.UtcNow;
        var saveSlot = new Data.Entities.SaveSlotEntity
        {
            Id = Guid.NewGuid(),
            Name = party.Leader.Name,
            CreatedAtUtc = now,
            LastPlayedAtUtc = now,
            Steps = party.Steps,
            Currency = party.Inventory.Currency
        };

        saveSlot.PartyMembers.AddRange(_mapper.CreatePartyMemberEntities(saveSlot.Id, party));
        saveSlot.InventoryStacks.AddRange(_mapper.CreateInventoryStackEntities(saveSlot.Id, party.Inventory));

        db.SaveSlots.Add(saveSlot);
        db.SaveChanges();

        return new GameSession(saveSlot.Id, party, null);
    }

    public GameSession LoadSave(Guid saveSlotId)
    {
        using var db = CreateContext();
        var saveSlot = LoadFullSaveSlot(db, saveSlotId)
            ?? throw new KeyNotFoundException($"Save slot '{saveSlotId}' was not found.");

        if (saveSlot.PartyMembers.Count == 0)
        {
            throw new InvalidOperationException($"Save slot '{saveSlotId}' has no party members.");
        }

        var party = _mapper.ToParty(saveSlot);
        var activeWorld = saveSlot.WorldSessions
            .Where(session => !session.IsCompleted)
            .OrderByDescending(session => session.UpdatedAtUtc)
            .FirstOrDefault();

        return new GameSession(
            saveSlot.Id,
            party,
            activeWorld is null ? null : _mapper.ToWorldSession(activeWorld));
    }

    public void Save(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        using var db = CreateContext();
        var saveSlot = LoadFullSaveSlot(db, session.SaveSlotId)
            ?? throw new KeyNotFoundException($"Save slot '{session.SaveSlotId}' was not found.");
        var now = DateTime.UtcNow;

        saveSlot.Name = session.Party.Leader.Name;
        saveSlot.LastPlayedAtUtc = now;
        saveSlot.Steps = session.Party.Steps;
        saveSlot.Currency = session.Party.Inventory.Currency;

        ReplacePartyInventory(db, saveSlot, session.Party);
        UpsertPartyMembers(db, saveSlot, session.Party);
        UpsertWorldSession(db, saveSlot, session.ActiveWorldSession);

        db.SaveChanges();
    }

    private MyTurnDbContext CreateContext()
    {
        return new MyTurnDbContext(_options);
    }

    private void ReplacePartyInventory(MyTurnDbContext db, Data.Entities.SaveSlotEntity saveSlot, Party party)
    {
        db.PartyInventoryStacks.RemoveRange(saveSlot.InventoryStacks);
        saveSlot.InventoryStacks.Clear();
        saveSlot.InventoryStacks.AddRange(_mapper.CreateInventoryStackEntities(saveSlot.Id, party.Inventory));
    }

    private void UpsertPartyMembers(MyTurnDbContext db, Data.Entities.SaveSlotEntity saveSlot, Party party)
    {
        var incoming = party.ActiveMembers
            .Select((actor, index) => (Actor: actor, Location: PartyMemberLocation.Active, ActiveOrder: (int?)index))
            .Concat(party.ReserveMembers.Select(actor => (Actor: actor, Location: PartyMemberLocation.Reserve, ActiveOrder: (int?)null)))
            .ToArray();
        var incomingIds = incoming.Select(member => member.Actor.Id).ToHashSet();

        foreach (var removed in saveSlot.PartyMembers.Where(member => !incomingIds.Contains(member.Id)).ToArray())
        {
            db.PartyMembers.Remove(removed);
        }

        foreach (var member in incoming)
        {
            var entity = saveSlot.PartyMembers.SingleOrDefault(existing => existing.Id == member.Actor.Id);

            if (entity is null)
            {
                saveSlot.PartyMembers.Add(_mapper.CreatePartyMemberEntity(
                    saveSlot.Id,
                    member.Actor,
                    member.Location,
                    member.ActiveOrder));
                continue;
            }

            db.PartyMemberStats.RemoveRange(entity.Stats);
            db.PartyMemberSkills.RemoveRange(entity.Skills);
            db.PartyMemberEquipment.RemoveRange(entity.Equipment);
            entity.Stats.Clear();
            entity.Skills.Clear();
            entity.Equipment.Clear();
            _mapper.UpdatePartyMemberEntity(entity, member.Actor, member.Location, member.ActiveOrder);
        }
    }

    private void UpsertWorldSession(
        MyTurnDbContext db,
        Data.Entities.SaveSlotEntity saveSlot,
        WorldSession? activeWorldSession)
    {
        if (activeWorldSession is null)
        {
            return;
        }

        var worldSession = db.WorldSessions
            .Include(world => world.Rooms)
            .SingleOrDefault(world => world.Id == activeWorldSession.Id);

        if (worldSession is null)
        {
            db.WorldSessions.Add(_mapper.CreateWorldSessionEntity(saveSlot.Id, activeWorldSession));
        }
        else
        {
            db.WorldRooms.RemoveRange(worldSession.Rooms);
            _mapper.UpdateWorldSessionEntity(worldSession, activeWorldSession);
        }
    }

    private static Data.Entities.SaveSlotEntity? LoadFullSaveSlot(MyTurnDbContext db, Guid saveSlotId)
    {
        return db.SaveSlots
            .Include(slot => slot.InventoryStacks)
            .Include(slot => slot.PartyMembers).ThenInclude(member => member.Stats)
            .Include(slot => slot.PartyMembers).ThenInclude(member => member.Skills)
            .Include(slot => slot.PartyMembers).ThenInclude(member => member.Equipment)
            .Include(slot => slot.WorldSessions).ThenInclude(world => world.Rooms)
            .SingleOrDefault(slot => slot.Id == saveSlotId);
    }
}
