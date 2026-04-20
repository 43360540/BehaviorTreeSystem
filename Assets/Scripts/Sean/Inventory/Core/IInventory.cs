using System;
using System.Collections.Generic;

namespace Sean.Inventory
{
    public interface IInventoryService
    {
        int Add(string id, int amount);
        int Remove(string id, int amount);
        int RemoveAt(int index, int amount);
        void SwapSlots(int indexA, int indexB);
    }
    
    public interface IReadOnlyInventory
    {
        event Action<int[]> InventoryChanged;
        int SlotCount { get; }
        Slot GetSlot(int index);
        int GetTotal(string id);
        bool HasAny(string id, out int amount);
    }
}