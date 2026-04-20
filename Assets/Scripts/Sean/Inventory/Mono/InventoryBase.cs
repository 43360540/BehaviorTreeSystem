using UnityEngine;

namespace Sean.Inventory
{
    public abstract class InventoryBase : MonoBehaviour
    {
        [Header("Base")]
        [SerializeField]    private GameObject _owner;
        [SerializeField, Min(1)] private int _capacity;
        
        protected Inventory Inventory { get; private set; }
        protected IItemInfoProvider InfoProvider { get; private set; }
        
        public IReadOnlyInventory ReadOnlyInventory { get; private set; }
        public IInventoryService InventorySvc { get; private set; }
        public int Capacity => _capacity;
        public GameObject Owner => _owner;

        
    #if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (_owner == null)
                _owner = this.gameObject;
        }
    #endif    
        
        private void Awake()
        {
            Inventory = new Inventory(Capacity);
            InfoProvider = CreateInfoProvider();
            
            ReadOnlyInventory = new ReadOnlyInventory(Inventory);
            InventorySvc = new InventoryService(Inventory, InfoProvider);

            OnInventoryReady();
        }
        
        protected abstract IItemInfoProvider CreateInfoProvider();
        
        protected virtual void OnInventoryReady() {}
    }
}