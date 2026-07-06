using Microsoft.EntityFrameworkCore;
using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Infrastructure.Data;

namespace MyTurn.Infrastructure.Catalog;

internal static class SqliteCatalogLoader
{
    public static CatalogSnapshot Load(MyTurnDbContext db)
    {
        var itemDefinitions = db.ItemDefinitions
            .AsNoTracking()
            .Include(item => item.StatModifiers)
            .OrderBy(item => item.Id)
            .AsEnumerable()
            .Select(CatalogMapper.ToDomain)
            .ToArray();

        var itemRegistry = new SqliteItemDefinitionRegistry(itemDefinitions);
        var weaponRegistry = new SqliteWeaponDefinitionRegistry(itemDefinitions.OfType<WeaponDefinition>());

        var enemyDefinitions = db.EnemyDefinitions
            .AsNoTracking()
            .Include(enemy => enemy.SpawnWeight)
            .Include(enemy => enemy.Stats)
            .Include(enemy => enemy.Actions)
            .Include(enemy => enemy.LootDrops)
            .OrderBy(enemy => enemy.Id)
            .AsEnumerable()
            .Select(enemy => CatalogMapper.ToDomain(enemy, itemRegistry))
            .ToArray();

        return new CatalogSnapshot(
            itemRegistry,
            weaponRegistry,
            new SqliteEnemyDefinitionRegistry(enemyDefinitions));
    }
}

internal sealed record CatalogSnapshot(
    IItemDefinitionRegistry ItemDefinitions,
    IWeaponDefinitionRegistry WeaponDefinitions,
    IEnemyDefinitionRegistry EnemyDefinitions);
