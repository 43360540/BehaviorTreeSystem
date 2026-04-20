using Item;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Sean.Button;

namespace Sean.Inventory
{
    // slot UI behavior
    public class SlotUi : DraggableButtonBase, IDropHandler, IPointerDownHandler
    {
        [Header("Slot UI")]
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private string _itemID;
        
        private InventoryUi _inventoryUi;
        private IReadOnlyInventory _inventory;
        private IInventoryService _inventorySvc;
        
        private bool _isInitialized = false;
        private int _index;
        
    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (_amount == null)
                _amount = GetComponentInChildren<TextMeshProUGUI>();
        }
    #endif
        
        private void Start()
        {
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (!_isInitialized)
            {
                Debug.LogError("Slot UI should haven't been successfully initialized");
                return;
            }
            Refresh();
        }

        public bool IsEmpty()
        {
            return _inventory.GetSlot(_index).IsEmpty;
        }

        public void Init(IReadOnlyInventory inventory, IInventoryService inventorySvc, 
                        InventoryUi inventoryUi, int index)
        {
            if (inventory == null || inventorySvc == null || inventoryUi == null)
            {
                Debug.LogError("Slot UI init failed");
                return;
            }
            
            _isInitialized = true;
            
            this._inventory = inventory;
            this._inventorySvc = inventorySvc;
            this._inventoryUi = inventoryUi;
            this._index = index;
        }

        private void Refresh()
        {
            Set(_index);
        }
        
        private void Set(int index)
        {
            if (_inventory == null) return;
            
            var slot = _inventory.GetSlot(this._index);

            if (slot.Amount <= 0 || string.IsNullOrEmpty(slot.ItemInfo.Id)) 
            {
                this._itemID = "empty";
                this._amount.text = "";
            }
            else
            {
                this._amount.text = slot.Amount.ToString();
                this._itemID = slot.ItemInfo.Id;
            }
            
            SetIcon(_itemID);
        }
        
        private void SetIcon(string id)
        {
            ItemDef target = ItemDatabase.Instance.GetItemDefByID(id);

            if (target != null) _button.sprite = target.icon;
            else return;
        }

        protected override bool IsDraggable()
        {
            return !_inventory.GetSlot(_index).IsEmpty;
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            if (_inventory == null) return;
            
            if (!eventData.pointerDrag.TryGetComponent<SlotUi>(out var dragged)
                || !this.TryGetComponent<SlotUi>(out var droppedOn)) 
                return;
            
            _inventorySvc.SwapSlots(dragged._index, droppedOn._index);
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            var slot = _inventory.GetSlot(_index);
            
            if (eventData.button != PointerEventData.InputButton.Right ||
                slot.IsEmpty)
                return;
            
            RightClickMenu.Instance.Activate(_inventoryUi.Owner, _index, slot.ItemInfo.Id);
        }
    }
}
