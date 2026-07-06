namespace MyTurn.Domain;

public sealed class Inventory
{
    private readonly Dictionary<string, InventoryStack> _items = [];

    public long Currency { get; private set; }
    public IReadOnlyCollection<InventoryStack> Items => _items.Values.ToArray();

    public void Add(IItemDefinition item, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (_items.TryGetValue(item.Id, out var existingStack))
        {
            existingStack.Add(quantity);
            return;
        }

        _items[item.Id] = new InventoryStack(item, quantity);
    }

    public bool Remove(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Item id is required.", nameof(itemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (!_items.TryGetValue(itemId, out var stack) || stack.Quantity < quantity)
        {
            return false;
        }

        stack.Remove(quantity);

        if (stack.Quantity == 0)
        {
            _items.Remove(itemId);
        }

        return true;
    }

    public bool TryGet(string itemId, out InventoryStack? stack)
    {
        return _items.TryGetValue(itemId, out stack);
    }

    public int GetQuantity(string itemId)
    {
        return _items.TryGetValue(itemId, out var stack) ? stack.Quantity : 0;
    }

    public void AddCurrency(long amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Currency amount cannot be negative.");
        }

        Currency += amount;
    }

    public bool SpendCurrency(long amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Currency amount cannot be negative.");
        }

        if (Currency < amount)
        {
            return false;
        }

        Currency -= amount;
        return true;
    }
}
