namespace Sean.Inventory
{
    public class InventoryService : IInventoryService
    {
        private readonly Inventory _inventory;
        private readonly IItemInfoProvider _infoProvider;
            
        public InventoryService(Inventory inventory, IItemInfoProvider infoProvider)
        {
            this._inventory = inventory;
            this._infoProvider = infoProvider;
        }
            
        public int Add(string id, int amount)
        {
            var info = _infoProvider.GetItemInfo(id);
            var actualAdded = _inventory.Add(info, amount);
                
            return actualAdded;
        }
    
        public int Remove(string id, int amount)
        {
            var info = _infoProvider.GetItemInfo(id);
            var actualRemoved = _inventory.Remove(info, amount);
                
            return actualRemoved;
        }
    
        public int RemoveAt(int index, int amount)
        {
            var actualRemoved = _inventory.RemoveAt(index, amount);
                
            return actualRemoved;
        }
    
        public void SwapSlots(int indexA, int indexB)
        {
            _inventory.SwapSlots(indexA, indexB);
        }
    
        public int GetTotal(string id)
        {
            return _inventory.GetTotal(id);
        }
    
        public bool HasAny(string id, out int amount)
        {
            return _inventory.HasAny(id, out amount);
        }
    }
}
