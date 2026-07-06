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

        using var db = CreateContext();
        var now = DateTime.UtcNow;
        var saveSlot = new Data.Entities.SaveSlotEntity
        {
            Id = Guid.NewGuid(),
            Name = actor.Name,
            CreatedAtUtc = now,
            LastPlayedAtUtc = now
        };
        saveSlot.Player = _mapper.CreatePlayerEntity(saveSlot.Id, actor);

        db.SaveSlots.Add(saveSlot);
        db.SaveChanges();

        return new GameSession(saveSlot.Id, actor, null);
    }

    public GameSession LoadSave(Guid saveSlotId)
    {
        using var db = CreateContext();
        var saveSlot = LoadFullSaveSlot(db, saveSlotId)
            ?? throw new KeyNotFoundException($"Save slot '{saveSlotId}' was not found.");

        if (saveSlot.Player is null)
        {
            throw new InvalidOperationException($"Save slot '{saveSlotId}' has no player record.");
        }

        var actor = _mapper.ToActor(saveSlot.Player);
        var activeWorld = saveSlot.WorldSessions
            .Where(session => !session.IsCompleted)
            .OrderByDescending(session => session.UpdatedAtUtc)
            .FirstOrDefault();

        return new GameSession(
            saveSlot.Id,
            actor,
            activeWorld is null ? null : _mapper.ToWorldSession(activeWorld));
    }

    public void Save(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        using var db = CreateContext();
        var saveSlot = LoadFullSaveSlot(db, session.SaveSlotId)
            ?? throw new KeyNotFoundException($"Save slot '{session.SaveSlotId}' was not found.");
        var now = DateTime.UtcNow;

        saveSlot.Name = session.Actor.Name;
        saveSlot.LastPlayedAtUtc = now;

        if (saveSlot.Player is null)
        {
            saveSlot.Player = _mapper.CreatePlayerEntity(saveSlot.Id, session.Actor);
        }
        else
        {
            db.PlayerStats.RemoveRange(saveSlot.Player.Stats);
            db.PlayerSkills.RemoveRange(saveSlot.Player.Skills);
            db.PlayerInventoryStacks.RemoveRange(saveSlot.Player.InventoryStacks);
            db.PlayerEquipment.RemoveRange(saveSlot.Player.Equipment);
            _mapper.UpdatePlayerEntity(saveSlot.Player, session.Actor);
        }

        if (session.ActiveWorldSession is not null)
        {
            var worldSession = db.WorldSessions
                .Include(world => world.Rooms)
                .SingleOrDefault(world => world.Id == session.ActiveWorldSession.Id);

            if (worldSession is null)
            {
                db.WorldSessions.Add(_mapper.CreateWorldSessionEntity(saveSlot.Id, session.ActiveWorldSession));
            }
            else
            {
                db.WorldRooms.RemoveRange(worldSession.Rooms);
                _mapper.UpdateWorldSessionEntity(worldSession, session.ActiveWorldSession);
            }
        }

        db.SaveChanges();
    }

    private MyTurnDbContext CreateContext()
    {
        return new MyTurnDbContext(_options);
    }

    private static Data.Entities.SaveSlotEntity? LoadFullSaveSlot(MyTurnDbContext db, Guid saveSlotId)
    {
        return db.SaveSlots
            .Include(slot => slot.Player)!.ThenInclude(player => player!.Stats)
            .Include(slot => slot.Player)!.ThenInclude(player => player!.Skills)
            .Include(slot => slot.Player)!.ThenInclude(player => player!.InventoryStacks)
            .Include(slot => slot.Player)!.ThenInclude(player => player!.Equipment)
            .Include(slot => slot.WorldSessions).ThenInclude(world => world.Rooms)
            .SingleOrDefault(slot => slot.Id == saveSlotId);
    }
}
