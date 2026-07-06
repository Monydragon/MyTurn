namespace MyTurn.Domain;

public sealed class InventoryStack
{
    public IItemDefinition Item { get; }
    public int Quantity { get; private set; }

    public InventoryStack(IItemDefinition item, int quantity)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        Quantity = quantity;
    }

    public void Add(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        Quantity += quantity;
    }

    public void Remove(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (quantity > Quantity)
        {
            throw new InvalidOperationException("Cannot remove more items than the stack contains.");
        }

        Quantity -= quantity;
    }
}
