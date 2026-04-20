using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Sean.Inventory
{
    // Managing slot prefabs
    public class InventoryUi : MonoBehaviour
    {
        [SerializeField] private SlotUi _ui;
        [SerializeField] private Transform _uiParent;
        [SerializeField] private GridLayoutGroup _grid;
        [SerializeField] private RightClickMenu _rm;

        private bool _isInitialized = false;
        
        public Dictionary<int, SlotUi> SlotUis { get; } = new();
        public IReadOnlyInventory Inventory { get; private set; }
        public IInventoryService InventorySvc { get; private set; }
        public GameObject Owner { get; private set; }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (_grid == null)
                _grid = GetComponentInChildren<GridLayoutGroup>();
            if (_grid != null && _uiParent == null)
                _uiParent = _grid.transform;
            if (_rm == null)
                _rm = GetComponentInChildren<RightClickMenu>();
        }
    #endif    
        
        private void Start()
        {
            _rm?.Close();
            DisableGridAsync().Forget();
        }

        private async UniTask DisableGridAsync()
        {
            if (_grid == null)
                return;
            
            await UniTask.WaitForEndOfFrame();
            _grid.enabled = false;
        }

        public void Initialize(InventoryBase inventory)
        {
            if (_isInitialized)
                throw new InvalidOperationException($"{name}: Init is called more than once. ({GetType()})");
            
            if (inventory == null)
            {
                Debug.LogError($"{typeof(Inventory)} cannot be null. ({GetType().Name})");
                return;
            }
            
            Owner = inventory.Owner;
            Inventory = inventory.ReadOnlyInventory;
            InventorySvc = inventory.InventorySvc;
            
            ClearAllUI();
            SlotUis.Clear();

            for (var i = 0; i < inventory.ReadOnlyInventory.SlotCount; i++)
            {
                var tempUi = Instantiate(_ui, _uiParent);
                tempUi.Init(Inventory, InventorySvc, this, i);
                SlotUis.Add(i, tempUi);
            }
            Inventory.InventoryChanged += HandleInventoryChanged;
            
            _isInitialized = true;
        }

        private void ClearAllUI()
        {
            foreach (var i in SlotUis)
            {
                Destroy(i.Value);
            }
        }
        
        private void HandleInventoryChanged(int[] indices)
        {
            foreach (var index in indices)
            {
                if(SlotUis.TryGetValue(index, out var ui))
                    ui.UpdateUI();
            }        
        }

        private void OnEnable()
        {
            if (Inventory != null)
            {
                Inventory.InventoryChanged -= HandleInventoryChanged;
                Inventory.InventoryChanged += HandleInventoryChanged;
            }
        }
        
        private void OnDisable()
        {
            if (Inventory != null)
                Inventory.InventoryChanged -= HandleInventoryChanged;
        }
    }
}
