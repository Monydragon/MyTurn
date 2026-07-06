using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class TreasureLootService : ITreasureLootService
{
    private readonly IItemDefinitionRegistry _items;

    public TreasureLootService(IItemDefinitionRegistry items)
    {
        _items = items;
    }

    public LootReward ClaimTreasure(Actor actor, WorldSession session, WorldRoom room)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(room);

        if (room.RoomType != RoomType.Treasure || room.IsLooted)
        {
            return LootReward.Empty;
        }

        var seed = DeriveRoomSeed(session.Map.Seed, room.Position);
        var random = new SeededRandomSource(seed);
        var currency = random.NextInt64Inclusive(8, 30);
        var item = random.NextInclusive(1, 100) <= 70
            ? _items.Get("small-healing-potion")
            : _items.Get("torn-cloth");
        var itemReward = new ItemReward(item, 1);
        var reward = new LootReward(currency, [itemReward]);

        actor.Inventory.AddCurrency(currency);
        actor.Inventory.Add(item, itemReward.Quantity);
        room.MarkLooted();

        return reward;
    }

    private static int DeriveRoomSeed(int worldSeed, WorldPosition position)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + worldSeed;
            hash = hash * 31 + position.X;
            hash = hash * 31 + position.Y;
            hash = hash * 31 + 7919;
            return hash;
        }
    }
}
