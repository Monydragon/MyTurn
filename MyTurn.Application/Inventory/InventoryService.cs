using MyTurn.Domain;

namespace MyTurn.Application;

public sealed class InventoryService : IInventoryService
{
    private readonly IItemDefinitionRegistry _items;

    public InventoryService(IItemDefinitionRegistry items)
    {
        _items = items;
    }

    public Inventory CreateStartingInventory(CharacterClass characterClass)
    {
        var inventory = new Inventory();
        inventory.Add(_items.Get("small-healing-potion"), 3);
        inventory.Add(_items.Get("cloth-tunic"));

        return inventory;
    }

    public void AddItem(Inventory inventory, string itemId, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        inventory.Add(_items.Get(itemId), quantity);
    }
}
