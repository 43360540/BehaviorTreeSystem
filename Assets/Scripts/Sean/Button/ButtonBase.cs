using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Sean.Button
{
    public abstract class ButtonBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Button")]   
        [SerializeField] protected Image _button;
        [SerializeField] protected bool _changeColorOnHover;
        [SerializeField] protected Color _activeColor = Color.white;
        [SerializeField] protected Color _inactiveColor = Color.gray7;
        [SerializeField] protected Color _selectedColor = Color.gray8;
        [SerializeField] protected bool _changeScaleOnHover;
        [SerializeField] protected float _selectedScale = 1.05f;

        [Header("Event")] 
        [SerializeField] private EventType _eventType = EventType.CsEvent;
        [SerializeField] protected UnityEvent _clickUnity;
        protected event Action ClickCs;
        
    #if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (_button == null)
                _button = GetComponentInChildren<Image>();
            if (_button != null)
                _button.color = _activeColor;
        }
    #endif

        protected virtual void Awake()
        {
            if (_button == null)
                Debug.LogError($"{name} is missing a Button.");
        }

        public virtual void SetActive(bool active)
        {
            if (active)
            {
                _button.raycastTarget = true;
                _button.color = _activeColor;
            }
            else
            {
                _button.raycastTarget = false;
                _button.color = _inactiveColor;
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_changeColorOnHover)
                _button.color = _selectedColor;
            
            if (_changeScaleOnHover)
                this.transform.localScale = Vector3.one * _selectedScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            this.transform.localScale = Vector3.one;
            _button.color = _activeColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleClick();
        }

        private void HandleClick()
        {
            switch (_eventType)
            {
                case EventType.None:
                    return;
                case EventType.UnityEvent:
                    _clickUnity?.Invoke();
                    break;
                case EventType.CsEvent:
                    ClickCs?.Invoke();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private enum EventType
        {
            None,
            UnityEvent,
            CsEvent,
        }
    }
}
