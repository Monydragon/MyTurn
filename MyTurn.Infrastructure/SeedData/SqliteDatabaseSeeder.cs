using Microsoft.EntityFrameworkCore;
using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Infrastructure.Catalog;
using MyTurn.Infrastructure.Data;
using MyTurn.Infrastructure.Data.Entities;

namespace MyTurn.Infrastructure.SeedData;

internal static class SqliteDatabaseSeeder
{
    public static void Seed(MyTurnDbContext db)
    {
        var items = DefaultCatalogData.Weapons
            .Cast<IItemDefinition>()
            .Concat(DefaultCatalogData.Items)
            .Concat(DefaultCatalogData.Enemies.Select(enemy => enemy.Enemy.Weapon).Cast<IItemDefinition>())
            .GroupBy(item => item.Id)
            .Select(group => group.First())
            .ToArray();

        foreach (var item in items)
        {
            UpsertItem(db, item);
        }

        foreach (var enemy in DefaultCatalogData.Enemies)
        {
            UpsertEnemy(db, enemy);
        }

        db.SaveChanges();
    }

    private static void UpsertItem(MyTurnDbContext db, IItemDefinition definition)
    {
        var incoming = CatalogMapper.ToEntity(definition);
        var existing = db.ItemDefinitions
            .Include(item => item.StatModifiers)
            .SingleOrDefault(item => item.Id == definition.Id);

        if (existing is null)
        {
            db.ItemDefinitions.Add(incoming);
            return;
        }

        existing.Name = incoming.Name;
        existing.Kind = incoming.Kind;
        existing.IsStackable = incoming.IsStackable;
        existing.WeaponType = incoming.WeaponType;
        existing.MinDamage = incoming.MinDamage;
        existing.MaxDamage = incoming.MaxDamage;
        existing.EquipmentSlot = incoming.EquipmentSlot;
        existing.HealingAmount = incoming.HealingAmount;

        db.ItemStatModifiers.RemoveRange(existing.StatModifiers);
        existing.StatModifiers = incoming.StatModifiers;
    }

    private static void UpsertEnemy(MyTurnDbContext db, WeightedEnemyDefinition definition)
    {
        var incoming = CatalogMapper.ToEntity(definition);
        var existing = db.EnemyDefinitions
            .Include(enemy => enemy.SpawnWeight)
            .Include(enemy => enemy.Stats)
            .Include(enemy => enemy.Actions)
            .Include(enemy => enemy.LootDrops)
            .SingleOrDefault(enemy => enemy.Id == definition.Enemy.Id);

        if (existing is null)
        {
            db.EnemyDefinitions.Add(incoming);
            return;
        }

        existing.Name = incoming.Name;
        existing.WeaponItemId = incoming.WeaponItemId;
        existing.ExperienceReward = incoming.ExperienceReward;

        if (existing.SpawnWeight is not null)
        {
            db.EnemySpawnWeights.Remove(existing.SpawnWeight);
        }

        db.EnemyStats.RemoveRange(existing.Stats);
        db.EnemyActions.RemoveRange(existing.Actions);
        db.EnemyLootDrops.RemoveRange(existing.LootDrops);

        existing.SpawnWeight = incoming.SpawnWeight;
        existing.Stats = incoming.Stats;
        existing.Actions = incoming.Actions;
        existing.LootDrops = incoming.LootDrops;
    }
}
