using MyTurn.Domain;

namespace MyTurn.Application;

public interface IInventoryService
{
    Inventory CreateStartingInventory(CharacterClass characterClass);
    void AddItem(Inventory inventory, string itemId, int quantity = 1);
}
