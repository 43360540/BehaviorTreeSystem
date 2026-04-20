using UnityEngine;
using UnityEngine.EventSystems;

namespace Sean.Button
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class DraggableButtonBase : ButtonBase, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Draggable Button")]
        [SerializeField] protected CanvasGroup _canvasGroup;
        [SerializeField] private Transform _defaultParent;
        
        private Canvas _canvas;
        private Vector3 _onBeginDragPosition;
        private bool _canDrag;
        
    #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }
    #endif   
        
        protected abstract bool IsDraggable();

        protected override void Awake()
        {
            base.Awake();
            
            _defaultParent = this.transform.parent;
            
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
                Debug.LogError($"No canvas found: ({GetType().Name} on {name})");
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            _canDrag = IsDraggable();
            if (!_canDrag) return;

            _onBeginDragPosition = this.transform.position;
            this.transform.SetParent(_canvas.transform);
            this.transform.SetAsLastSibling();
            _canvasGroup.blocksRaycasts = false;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!_canDrag) return;

            this.transform.position += (Vector3)eventData.delta;
            _canvasGroup.blocksRaycasts = false;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (!_canDrag) return;

            this.transform.SetParent(_defaultParent);
            this.transform.position = _onBeginDragPosition;
            _canvasGroup.blocksRaycasts = true;
        }
    }
}