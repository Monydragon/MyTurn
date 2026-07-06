using MyTurn.Application;
using MyTurn.Domain;
using MyTurn.Infrastructure.Data.Entities;

namespace MyTurn.Infrastructure.Catalog;

internal static class CatalogMapper
{
    public static ItemDefinitionEntity ToEntity(IItemDefinition definition)
    {
        var entity = new ItemDefinitionEntity
        {
            Id = definition.Id,
            Name = definition.Name,
            Kind = definition.Kind.ToString(),
            IsStackable = definition.IsStackable,
            Tier = 1,
            SuggestedLevel = 1
        };

        switch (definition)
        {
            case ConsumableDefinition consumable:
                entity.HealingAmount = consumable.HealingAmount;
                break;
            case ArmorDefinition armor:
                entity.EquipmentSlot = armor.Slot.ToString();
                entity.StatModifiers = ToModifierEntities(armor.Id, armor.StatModifiers);
                break;
            case WeaponDefinition weapon:
                entity.WeaponType = weapon.WeaponType.ToString();
                entity.MinDamage = weapon.MinDamage;
                entity.MaxDamage = weapon.MaxDamage;
                entity.EquipmentSlot = weapon.Slot.ToString();
                entity.StatModifiers = ToModifierEntities(weapon.Id, weapon.StatModifiers);
                break;
        }

        return entity;
    }

    public static IItemDefinition ToDomain(ItemDefinitionEntity entity)
    {
        var kind = Enum.Parse<ItemKind>(entity.Kind);

        return kind switch
        {
            ItemKind.Consumable => new ConsumableDefinition(
                entity.Id,
                entity.Name,
                entity.HealingAmount ?? 0),
            ItemKind.Material => new MaterialDefinition(entity.Id, entity.Name),
            ItemKind.Armor => new ArmorDefinition(
                entity.Id,
                entity.Name,
                ParseRequired<EquipmentSlot>(entity.EquipmentSlot, entity.Id, nameof(entity.EquipmentSlot)),
                ToModifierDefinitions(entity.StatModifiers)),
            ItemKind.Weapon => new WeaponDefinition(
                entity.Id,
                entity.Name,
                ParseRequired<WeaponType>(entity.WeaponType, entity.Id, nameof(entity.WeaponType)),
                entity.MinDamage ?? 1,
                entity.MaxDamage ?? Math.Max(1, entity.MinDamage ?? 1),
                ToModifierDefinitions(entity.StatModifiers)),
            _ => throw new InvalidOperationException($"Unsupported item kind '{entity.Kind}'.")
        };
    }

    public static EnemyDefinitionEntity ToEntity(WeightedEnemyDefinition weightedEnemy)
    {
        var enemy = weightedEnemy.Enemy;

        return new EnemyDefinitionEntity
        {
            Id = enemy.Id,
            Name = enemy.Name,
            WeaponItemId = enemy.Weapon.Id,
            ExperienceReward = enemy.ExperienceReward,
            ThreatRating = enemy.ThreatRating,
            SpawnWeight = new EnemySpawnWeightEntity
            {
                EnemyDefinitionId = enemy.Id,
                Weight = weightedEnemy.Weight
            },
            Stats = enemy.Stats
                .Select(stat => new EnemyStatEntity
                {
                    EnemyDefinitionId = enemy.Id,
                    StatType = stat.Key.ToString(),
                    Value = stat.Value
                })
                .ToList(),
            Actions = enemy.Actions
                .Select(action => new EnemyActionEntity
                {
                    EnemyDefinitionId = enemy.Id,
                    ActionType = action.ActionType.ToString(),
                    Weight = action.Weight
                })
                .ToList(),
            LootDrops = enemy.LootTable
                .Select(drop => new EnemyLootDropEntity
                {
                    EnemyDefinitionId = enemy.Id,
                    Kind = drop.Kind.ToString(),
                    ItemId = drop.ItemId,
                    MinQuantity = drop.MinQuantity,
                    MaxQuantity = drop.MaxQuantity,
                    Weight = drop.Weight
                })
                .ToList()
        };
    }

    public static WeightedEnemyDefinition ToDomain(EnemyDefinitionEntity entity, IItemDefinitionRegistry items)
    {
        if (items.Get(entity.WeaponItemId) is not IWeapon weapon)
        {
            throw new InvalidOperationException($"Enemy '{entity.Id}' references non-weapon item '{entity.WeaponItemId}'.");
        }

        var enemy = new EnemyDefinition(
            entity.Id,
            entity.Name,
            entity.Stats.ToDictionary(
                stat => Enum.Parse<StatType>(stat.StatType),
                stat => stat.Value),
            weapon,
            entity.ExperienceReward,
            entity.Actions
                .Select(action => new WeightedEnemyAction(
                    Enum.Parse<EnemyActionType>(action.ActionType),
                    action.Weight))
                .ToArray(),
            entity.LootDrops
                .Select(drop => new LootDropDefinition(
                    Enum.Parse<LootDropKind>(drop.Kind),
                    drop.ItemId,
                    drop.MinQuantity,
                    drop.MaxQuantity,
                    drop.Weight))
                .ToArray(),
            entity.ThreatRating);

        return new WeightedEnemyDefinition(enemy, entity.SpawnWeight?.Weight ?? 0);
    }

    private static List<ItemStatModifierEntity> ToModifierEntities(
        string itemId,
        IReadOnlyCollection<StatModifierDefinition> modifiers)
    {
        return modifiers
            .Select(modifier => new ItemStatModifierEntity
            {
                ItemDefinitionId = itemId,
                StatType = modifier.StatType.ToString(),
                Value = modifier.Value
            })
            .ToList();
    }

    private static IReadOnlyCollection<StatModifierDefinition> ToModifierDefinitions(
        IEnumerable<ItemStatModifierEntity> modifiers)
    {
        return modifiers
            .Select(modifier => new StatModifierDefinition(
                Enum.Parse<StatType>(modifier.StatType),
                modifier.Value))
            .ToArray();
    }

    private static TEnum ParseRequired<TEnum>(string? value, string itemId, string columnName)
        where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Item '{itemId}' is missing required '{columnName}'.");
        }

        return Enum.Parse<TEnum>(value);
    }
}
