using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class LootService : ILootService
{
    private readonly IItemDefinitionRegistry _items;

    public LootService(IItemDefinitionRegistry items)
    {
        _items = items;
    }

    public LootReward RollLoot(IEnumerable<EnemyDefinition> enemies, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(random);

        var currency = 0L;
        var itemQuantities = new Dictionary<string, (IItemDefinition Item, int Quantity)>();

        foreach (var enemy in enemies)
        {
            var drop = ChooseDrop(enemy.LootTable, random);

            if (drop is null)
            {
                continue;
            }

            var quantity = random.NextInt64Inclusive(drop.MinQuantity, drop.MaxQuantity);

            if (drop.Kind == LootDropKind.Currency)
            {
                currency += quantity;
                continue;
            }

            var item = _items.Get(drop.ItemId);
            var itemQuantity = checked((int)quantity);

            if (itemQuantities.TryGetValue(item.Id, out var existing))
            {
                itemQuantities[item.Id] = (item, existing.Quantity + itemQuantity);
            }
            else
            {
                itemQuantities[item.Id] = (item, itemQuantity);
            }
        }

        return new LootReward(
            currency,
            itemQuantities.Values.Select(value => new ItemReward(value.Item, value.Quantity)).ToArray());
    }

    private static LootDropDefinition? ChooseDrop(IReadOnlyCollection<LootDropDefinition> lootTable, IRandomSource random)
    {
        var totalWeight = lootTable.Sum(drop => Math.Max(0, drop.Weight));

        if (totalWeight <= 0)
        {
            return null;
        }

        var roll = random.NextInclusive(1, totalWeight);
        var current = 0;

        foreach (var drop in lootTable)
        {
            current += Math.Max(0, drop.Weight);

            if (roll <= current)
            {
                return drop;
            }
        }

        return null;
    }
}
