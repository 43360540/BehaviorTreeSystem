using System;
using System.Collections.Generic;

namespace Sean.Inventory
{
    // managing slots
    public class Inventory
    {
        public Inventory(int capacity)
        {
            SetCapacity(capacity);
            EnsureCapacity();
        }
        
        public event Action<int[]> InventoryChanged;
        public List<Slot> Slots { get; private set; } = new();
        
        private int _capacity = 10;

        private void SetCapacity(int capacity)
        {
            if (capacity <= 0)
            {
                return;
            }
            _capacity = capacity;
        }

        private void EnsureCapacity()
        {
            if (_capacity < 1) _capacity = 1;
            // if no list, create one
            if (Slots == null) Slots = new List<Slot>(_capacity);
            // complete
            while (Slots.Count < _capacity)
            {
                var slot = new Slot();
                slot.Init();
                Slots.Add(slot);
            }            
            // trim
            if (Slots.Count > _capacity) Slots.RemoveRange(_capacity, (Slots.Count - _capacity));
        }

        public int Add(ItemInfo info, int amount)
        {
            if (info.Id == null || amount <= 0) return 0;

            int remaining = amount;

            //1st: try stacking with existing stacks
            for (int i = 0; i < Slots.Count && remaining > 0; i++)
            {
                var slot = Slots[i];
                int current = remaining;

                if (!slot.IsEmpty && slot.ItemInfo.Id == info.Id && info.IsStackable)
                {
                    int added = slot.TryAdd(info, remaining);
                    remaining -= added;
                }

                if (remaining != current)
                    InventoryChanged?.Invoke(new int[]{i});          
            }

            // 2nd: store the rest in empty slots
            for (int i = 0; i < Slots.Count && remaining > 0; i++)
            {
                var x = Slots[i];
                int current = remaining;

                if (x.IsEmpty)
                {
                    int added = x.TryAdd(info, remaining);
                    remaining -= added;
                }

                if (remaining != current)
                    InventoryChanged?.Invoke(new int[]{i});
            }

            int actualAdded = amount - remaining;

            return actualAdded;
        }

        public int Remove(ItemInfo info, int amount)
        {
            if (info.Id == null || amount <= 0) return 0;

            int remaining = amount;
            int current = remaining;

            // remove from all slots until amount is satisfied
            for (int i = 0; i < Slots.Count && remaining > 0; i++)
            {
                Slot slot = Slots[i];
                if (info.Id == slot.ItemInfo.Id)
                {
                    int took = slot.TryRemove(info, remaining);
                    remaining -= took;
                }
                if (remaining != current)
                    InventoryChanged?.Invoke(new int[]{i});
            }

            int actualRemoved = amount - remaining;

            return actualRemoved;
        }

        // Remove at specific slot
        public int RemoveAt(int index, int amount)
        {
            if (amount <= 0 || Slots[index].IsEmpty) return 0;

            int took = Slots[index].TryRemove(amount);

            if (took > 0) InventoryChanged?.Invoke(new int[]{index});

            return took;
        }

        /// <summary>
        ///     Swap two slots directly
        /// </summary>
        public void SwapSlots(int indexA, int indexB)
        {
            // ensure index 
            if (indexA == indexB) return;

            if (Slots[indexA].IsEmpty && Slots[indexB].IsEmpty)
                return;

            (Slots[indexA], Slots[indexB]) = (Slots[indexB], Slots[indexA]);

            InventoryChanged?.Invoke(new int[]{indexA, indexB});
        }

        // Get item's total from inventory
        public int GetTotal(string id)
        {
            if (id == null) return 0;

            int total = 0;

            for (int i = 0; i < Slots.Count; i++)
            {
                var x = Slots[i];

                if (!x.IsEmpty && x.ItemInfo.Id == id) 
                    total += x.Amount;
            }

            return total;
        }

        public bool HasAny(string id, out int amount)
        {
            if (id == null)
            {
                amount = 0;
                return false;
            }            
            amount = GetTotal(id);
            return amount > 0;
        }
    }
}

