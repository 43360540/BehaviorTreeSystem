using Item;
using UnityEngine;
using Sean.Ui;
using UnityEngine.EventSystems;

namespace Sean.Inventory
{
    public class RightClickMenu : MonoBehaviour, IPointerClickHandler
    {
        public static RightClickMenu Instance;

        [SerializeField] private Transform _parent;
        [SerializeField] private CanvasGroup _cg;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _canvasRect;
        
        [Header("Buttons")]
        [SerializeField] private RmButton _useButton;
        [SerializeField] private RmButton _equipButton;
        [SerializeField] private RmButton _unequipButton;

        private GameObject _owner;
        private int _index;
        private ItemDef _item;

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (_parent == null)
                _parent = this.transform.parent;
            if (_cg == null && _parent != null)
                _cg = _parent.GetComponent<CanvasGroup>();
            if (_canvas ==  null)
                _canvas = GetComponentInParent<Canvas>();
            if (_canvasRect == null && _canvas != null)
                _canvasRect = _canvas.GetComponent<RectTransform>();
        }
    #endif    
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            Instance = this;

            _useButton.SetClickCallback(Use);
            _equipButton.SetClickCallback(Equip);
            _unequipButton.SetClickCallback(Unequip);
            
            Close();
        }

        public void Activate(GameObject inventoryOwner, int slotIndex, string itemId)
        {
            _owner = inventoryOwner;
            _item = ItemDatabase.Instance.GetItemDefByID(itemId);
            _index = slotIndex;
            SetButton();
            Open();
        }

        private void SetButton()
        {
            _useButton.SetActive(_item is IUsable);
            _equipButton.SetActive(_item is IEquippable);
            _unequipButton.SetActive(_item is IEquippable);
        }

        private void Use()
        {
            if (_item is IUsable usable)
            {
                usable.Use(
                    new UseContext(_owner, _index));
            }                
            Close();
        }

        private void Equip()
        {
            if (_item is IEquippable equippable)
            {
                equippable.Equip(
                    new EquipContext(_owner, _index));
            }                
            Close();
        }
        
        private void Unequip()
        {
            if (_item is IEquippable equippable)
            {
                equippable.Unequip(
                    new EquipContext(_owner, _index));
            }                
            Close();
        }

        private Vector2 GetMousePosition()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, Input.mousePosition, null, out var position);
            return position;
        }

        public void Open()
        {
            UiTool.Set(_cg, true, GetMousePosition(), 0.1f);
        }
        
        public void Close()
        {
            UiTool.Set(_cg, false, 0.1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log(eventData.pointerClick.gameObject.name);
            if (eventData.pointerClick.gameObject != this.gameObject)
                Close();
        }
    }
}

