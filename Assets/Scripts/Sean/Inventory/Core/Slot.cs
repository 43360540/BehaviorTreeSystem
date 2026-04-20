using System;

// slot data
namespace Sean.Inventory
{
    [Serializable]
    public class Slot
    {
        private ItemInfo _itemInfo;
        private int _amount = 0;

        // read only info
        public ItemInfo ItemInfo => _itemInfo;
        public int Amount => _amount;
        public bool IsEmpty => string.IsNullOrEmpty(_itemInfo.Id) || _amount <= 0;

        private void Set(ItemInfo item, int itemAmount)
        {
            _itemInfo = item;
            _amount = itemAmount;
            Normalize();
        }

        private void Add(int toAdd)
        {
            _amount += toAdd;
            Normalize();
        }

        private void Remove(int toRemove)
        {
            _amount -= toRemove;
            Normalize();
        }

        private void Reset()
        {
            _itemInfo = new ItemInfo();
            _amount = 0;
        }

        public void Init()
        {
            Normalize();
        }

        public void Normalize()
        {
            // No item or item is "empty item"
            if (IsEmpty)
            {
                Reset();
                return;
            }

            if (_itemInfo.IsStackable)
            {
                int max = Math.Max(1, _itemInfo.MaxStack);
                _amount = Math.Clamp(_amount, 0, max);
            }
            else // if item isn't stackable
            {
                _amount = 1;
            }
        }

        public int SpaceLeftFor(ItemInfo info)
        {
            if (IsEmpty) 
                return info.IsStackable ? Math.Max(1, info.MaxStack) : 1;
            if (info.Id != _itemInfo.Id) return 0;
            if (!info.IsStackable) return 0;

            return Math.Max(0, info.MaxStack - _amount);
        }

        // return true if SpaceLeftFor(def) >= 1
        public bool CanAcceptAtLeastOne(ItemInfo info) => 
            SpaceLeftFor(info) >= 1;

        public int TryAdd(ItemInfo info, int amount)
        {
            if (info.Id == null || amount <= 0) return 0;

            if (IsEmpty)
            {
                if (info.IsStackable)
                {
                    int toAdd = Math.Min(amount, Math.Max(1, info.MaxStack));
                    Set(info, toAdd);

                    return toAdd;
                }
                else
                {
                    Set(info, 1);
                    return 1;
                }
            }

            // not empty: stack with the "same stackable-item" only
            if (info.Id != _itemInfo.Id) return 0;

            int space = info.MaxStack - this._amount;
            int add = Math.Min(space, amount);
            Add(add);

            return add;
        }

        public int TryRemove(ItemInfo info, int amount)
        {
            if (IsEmpty) return 0;
            if (info.Id == null || amount <= 0) return 0;
            if (info.Id != _itemInfo.Id) return 0;

            int toTake = Math.Min(amount, this._amount);
            Remove(toTake);

            return toTake;
        }

        public int TryRemove(int amount)
        {
            if (IsEmpty) return 0;
            if (amount <= 0) return 0;

            int toTake = Math.Min(amount, this._amount);
            Remove(toTake);

            return toTake;
        }
    }
}


