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
    public DbSet<PartyMemberEntity> PartyMembers => Set<PartyMemberEntity>();
    public DbSet<PartyMemberStatEntity> PartyMemberStats => Set<PartyMemberStatEntity>();
    public DbSet<PartyMemberSkillEntity> PartyMemberSkills => Set<PartyMemberSkillEntity>();
    public DbSet<PartyInventoryStackEntity> PartyInventoryStacks => Set<PartyInventoryStackEntity>();
    public DbSet<PartyMemberEquipmentEntity> PartyMemberEquipment => Set<PartyMemberEquipmentEntity>();
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
            entity.Property(item => item.Tier).HasDefaultValue(1);
            entity.Property(item => item.SuggestedLevel).HasDefaultValue(1);
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
            entity.Property(enemy => enemy.ThreatRating).HasDefaultValue(1);
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
            entity.HasMany(slot => slot.PartyMembers)
                .WithOne(member => member.SaveSlot)
                .HasForeignKey(member => member.SaveSlotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(slot => slot.InventoryStacks)
                .WithOne(stack => stack.SaveSlot)
                .HasForeignKey(stack => stack.SaveSlotId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(slot => slot.WorldSessions)
                .WithOne(session => session.SaveSlot)
                .HasForeignKey(session => session.SaveSlotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PartyMemberEntity>(entity =>
        {
            entity.HasKey(member => member.Id);
            entity.HasIndex(member => new { member.SaveSlotId, member.ActiveOrder }).IsUnique();
            entity.Property(member => member.Name).HasMaxLength(200).IsRequired();
            entity.Property(member => member.Gender).HasMaxLength(64).IsRequired();
            entity.Property(member => member.Species).HasMaxLength(64).IsRequired();
            entity.Property(member => member.CharacterClass).HasMaxLength(64).IsRequired();
            entity.Property(member => member.Location).HasMaxLength(64).IsRequired();
            entity.HasMany(member => member.Stats)
                .WithOne(stat => stat.PartyMember)
                .HasForeignKey(stat => stat.PartyMemberId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(member => member.Skills)
                .WithOne(skill => skill.PartyMember)
                .HasForeignKey(skill => skill.PartyMemberId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(member => member.Equipment)
                .WithOne(equipment => equipment.PartyMember)
                .HasForeignKey(equipment => equipment.PartyMemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PartyMemberStatEntity>(entity =>
        {
            entity.HasKey(stat => stat.Id);
            entity.Property(stat => stat.StatType).HasMaxLength(64).IsRequired();
            entity.HasIndex(stat => new { stat.PartyMemberId, stat.StatType }).IsUnique();
        });

        modelBuilder.Entity<PartyMemberSkillEntity>(entity =>
        {
            entity.HasKey(skill => skill.Id);
            entity.Property(skill => skill.SkillType).HasMaxLength(64).IsRequired();
            entity.Property(skill => skill.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(skill => new { skill.PartyMemberId, skill.SkillType }).IsUnique();
        });

        modelBuilder.Entity<PartyInventoryStackEntity>(entity =>
        {
            entity.HasKey(stack => stack.Id);
            entity.Property(stack => stack.ItemId).HasMaxLength(128).IsRequired();
            entity.HasIndex(stack => new { stack.SaveSlotId, stack.ItemId }).IsUnique();
            entity.HasOne(stack => stack.ItemDefinition)
                .WithMany()
                .HasForeignKey(stack => stack.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PartyMemberEquipmentEntity>(entity =>
        {
            entity.HasKey(equipment => equipment.Id);
            entity.Property(equipment => equipment.Slot).HasMaxLength(64).IsRequired();
            entity.Property(equipment => equipment.ItemId).HasMaxLength(128).IsRequired();
            entity.HasIndex(equipment => new { equipment.PartyMemberId, equipment.Slot }).IsUnique();
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
