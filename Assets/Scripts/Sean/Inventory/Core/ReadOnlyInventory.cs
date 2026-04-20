using System;

namespace Sean.Inventory
{
    public class ReadOnlyInventory : IReadOnlyInventory
    {
        public ReadOnlyInventory(Inventory inventory)
        {
            this._inventory = inventory;
        }
        
        private readonly Inventory _inventory;
        
        public int SlotCount => _inventory.Slots.Count;
        
        public Slot GetSlot(int index)
        {
            return _inventory.Slots[index];
        }

        public int GetTotal(string id)
        {
            return _inventory.GetTotal(id);
        }

        public bool HasAny(string id, out int amount)
        {
            return _inventory.HasAny(id, out amount);
        }
        
        public event Action<int[]> InventoryChanged
        {
            add => _inventory.InventoryChanged += value;
            remove => _inventory.InventoryChanged -= value;
        }
    }
}