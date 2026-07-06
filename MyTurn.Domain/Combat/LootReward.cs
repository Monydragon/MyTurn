namespace MyTurn.Domain;

public sealed record LootReward(
    long Currency,
    IReadOnlyList<ItemReward> Items)
{
    public static LootReward Empty { get; } = new(0, []);
}
