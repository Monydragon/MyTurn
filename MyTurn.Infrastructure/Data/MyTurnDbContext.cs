using Microsoft.EntityFrameworkCore;
using MyTurn.Infrastructure.Data.Entities;

namespace MyTurn.Infrastructure.Data;

public sealed class MyTurnDbContext : DbContext
{
    public MyTurnDbContext(DbContextOptions<MyTurnDbContext> options)
        : base(options)
    {
    }

    public DbSet<ItemDefinitionEntity> ItemDefinitions => Set<ItemDefinitionEntity>();
    public DbSet<ItemStatModifierEntity> ItemStatModifiers => Set<ItemStatModifierEntity>();
    public DbSet<EnemyDefinitionEntity> EnemyDefinitions => Set<EnemyDefinitionEntity>();
    public DbSet<EnemyStatEntity> EnemyStats => Set<EnemyStatEntity>();
    public DbSet<EnemyActionEntity> EnemyActions => Set<EnemyActionEntity>();
    public DbSet<EnemyLootDropEntity> EnemyLootDrops => Set<EnemyLootDropEntity>();
    public DbSet<EnemySpawnWeightEntity> EnemySpawnWeights => Set<EnemySpawnWeightEntity>();
    public DbSet<SaveSlotEntity> SaveSlots => Set<SaveSlotEntity>();
    public DbSet<PlayerEntity> Players => Set<PlayerEntity>();
    public DbSet<PlayerStatEntity> PlayerStats => Set<PlayerStatEntity>();
    public DbSet<PlayerSkillEntity> PlayerSkills => Set<PlayerSkillEntity>();
    public DbSet<PlayerInventoryStackEntity> PlayerInventoryStacks => Set<PlayerInventoryStackEntity>();
    public DbSet<PlayerEquipmentEntity> PlayerEquipment => Set<PlayerEquipmentEntity>();
    public DbSet<WorldSessionEntity> WorldSessions => Set<WorldSessionEntity>();
    public DbSet<WorldRoomEntity> WorldRooms => Set<WorldRoomEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemDefinitionEntity>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasMaxLength(128);
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Kind).HasMaxLength(64).IsRequired();
            entity.Property(item => item.WeaponType).HasMaxLength(64);
            entity.Property(item => item.EquipmentSlot).HasMaxLength(64);
            entity.HasMany(item => item.StatModifiers)
                .WithOne(modifier => modifier.ItemDefinition)
                .HasForeignKey(modifier => modifier.ItemDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemStatModifierEntity>(entity =>
        {
            entity.HasKey(modifier => modifier.Id);
            entity.Property(modifier => modifier.ItemDefinitionId).HasMaxLength(128).IsRequired();
            entity.Property(modifier => modifier.StatType).HasMaxLength(64).IsRequired();
            entity.HasIndex(modifier => new { modifier.ItemDefinitionId, modifier.StatType, modifier.Value });
        });

        modelBuilder.Entity<EnemyDefinitionEntity>(entity =>
        {
            entity.HasKey(enemy => enemy.Id);
            entity.Property(enemy => enemy.Id).HasMaxLength(128);
            entity.Property(enemy => enemy.Name).HasMaxLength(200).IsRequired();
            entity.Property(enemy => enemy.WeaponItemId).HasMaxLength(128).IsRequired();
            entity.HasOne(enemy => enemy.WeaponItem)
                .WithMany()
                .HasForeignKey(enemy => enemy.WeaponItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(enemy => enemy.Stats)
                .WithOne(stat => stat.EnemyDefinition)
                .HasForeignKey(stat => stat.EnemyDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(enemy => enemy.Actions)
                .WithOne(action => action.EnemyDefinition)
                .HasForeignKey(action => action.EnemyDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(enemy => enemy.LootDrops)
                .WithOne(drop => drop.EnemyDefinition)
                .HasForeignKey(drop => drop.EnemyDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(enemy => enemy.SpawnWeight)
                .WithOne(weight => weight.EnemyDefinition)
                .HasForeignKey<EnemySpawnWeightEntity>(weight => weight.EnemyDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnemyStatEntity>(entity =>
        {
            entity.HasKey(stat => stat.Id);
            entity.Property(stat => stat.EnemyDefinitionId).HasMaxLength(128).IsRequired();
            entity.Property(stat => stat.StatType).HasMaxLength(64).IsRequired();
            entity.HasIndex(stat => new { stat.EnemyDefinitionId, stat.StatType }).IsUnique();
        });

        modelBuilder.Entity<EnemyActionEntity>(entity =>
        {
            entity.HasKey(action => action.Id);
            entity.Property(action => action.EnemyDefinitionId).HasMaxLength(128).IsRequired();
            entity.Property(action => action.ActionType).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<EnemyLootDropEntity>(entity =>
        {
            entity.HasKey(drop => drop.Id);
            entity.Property(drop => drop.EnemyDefinitionId).HasMaxLength(128).IsRequired();
            entity.Property(drop => drop.Kind).HasMaxLength(64).IsRequired();
            entity.Property(drop => drop.ItemId).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<EnemySpawnWeightEntity>(entity =>
        {
            entity.HasKey(weight => weight.Id);
            entity.Property(weight => weight.EnemyDefinitionId).HasMaxLength(128).IsRequired();
            entity.HasIndex(weight => weight.EnemyDefinitionId).IsUnique();
        });

        modelBuilder.Entity<SaveSlotEntity>(entity =>
        {
            entity.HasKey(slot => slot.Id);
            entity.Property(slot => slot.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(slot => slot.Player)
                .WithOne(player => player.SaveSlot)
                .HasForeignKey<PlayerEntity>(player => player.SaveSlotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(slot => slot.WorldSessions)
                .WithOne(session => session.SaveSlot)
                .HasForeignKey(session => session.SaveSlotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerEntity>(entity =>
        {
            entity.HasKey(player => player.Id);
            entity.HasIndex(player => player.SaveSlotId).IsUnique();
            entity.Property(player => player.Name).HasMaxLength(200).IsRequired();
            entity.Property(player => player.Gender).HasMaxLength(64).IsRequired();
            entity.Property(player => player.Species).HasMaxLength(64).IsRequired();
            entity.Property(player => player.CharacterClass).HasMaxLength(64).IsRequired();
            entity.HasMany(player => player.Stats)
                .WithOne(stat => stat.Player)
                .HasForeignKey(stat => stat.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(player => player.Skills)
                .WithOne(skill => skill.Player)
                .HasForeignKey(skill => skill.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(player => player.InventoryStacks)
                .WithOne(stack => stack.Player)
                .HasForeignKey(stack => stack.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(player => player.Equipment)
                .WithOne(equipment => equipment.Player)
                .HasForeignKey(equipment => equipment.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerStatEntity>(entity =>
        {
            entity.HasKey(stat => stat.Id);
            entity.Property(stat => stat.StatType).HasMaxLength(64).IsRequired();
            entity.HasIndex(stat => new { stat.PlayerId, stat.StatType }).IsUnique();
        });

        modelBuilder.Entity<PlayerSkillEntity>(entity =>
        {
            entity.HasKey(skill => skill.Id);
            entity.Property(skill => skill.SkillType).HasMaxLength(64).IsRequired();
            entity.Property(skill => skill.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(skill => new { skill.PlayerId, skill.SkillType }).IsUnique();
        });

        modelBuilder.Entity<PlayerInventoryStackEntity>(entity =>
        {
            entity.HasKey(stack => stack.Id);
            entity.Property(stack => stack.ItemId).HasMaxLength(128).IsRequired();
            entity.HasIndex(stack => new { stack.PlayerId, stack.ItemId }).IsUnique();
            entity.HasOne(stack => stack.ItemDefinition)
                .WithMany()
                .HasForeignKey(stack => stack.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlayerEquipmentEntity>(entity =>
        {
            entity.HasKey(equipment => equipment.Id);
            entity.Property(equipment => equipment.Slot).HasMaxLength(64).IsRequired();
            entity.Property(equipment => equipment.ItemId).HasMaxLength(128).IsRequired();
            entity.HasIndex(equipment => new { equipment.PlayerId, equipment.Slot }).IsUnique();
            entity.HasOne(equipment => equipment.ItemDefinition)
                .WithMany()
                .HasForeignKey(equipment => equipment.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorldSessionEntity>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.HasIndex(session => new { session.SaveSlotId, session.IsCompleted });
            entity.HasMany(session => session.Rooms)
                .WithOne(room => room.WorldSession)
                .HasForeignKey(room => room.WorldSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorldRoomEntity>(entity =>
        {
            entity.HasKey(room => room.Id);
            entity.Property(room => room.RoomType).HasMaxLength(64).IsRequired();
            entity.HasIndex(room => new { room.WorldSessionId, room.X, room.Y }).IsUnique();
        });
    }
}
