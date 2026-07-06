namespace MyTurn.Domain;

public sealed record LootDropDefinition(
    LootDropKind Kind,
    string ItemId,
    long MinQuantity,
    long MaxQuantity,
    int Weight);
