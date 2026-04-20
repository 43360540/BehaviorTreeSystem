using Sean.Inventory;
using UnityEngine;

namespace Item
{
    public struct EquipContext
    {
        public EquipContext(GameObject user, int slotIndex)
        {
            this.User = user;
            this.SlotIndex = slotIndex;
        }
        
        public GameObject User {get; private set;}
        public int SlotIndex {get; private set;}
    }
    
    public struct UseContext
    {
        public UseContext(GameObject user, int slotIndex)
        {
            this.User = user;
            this.SlotIndex = slotIndex;
        }
        
        public GameObject User {get; private set;}
        public int SlotIndex {get; private set;}
    }
}
