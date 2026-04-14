using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts
{
    public class DraggableItem : MonoBehaviour,  IPointerDownHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float longPressTime = 0.2f;

        private ScrollRect _parentScrollRect;
        private Canvas _rootCanvas;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        private Transform _originalParent;
        private int _originalIndex;

        private float _pointerDownTime;
        private bool _routeToScroll;
        private bool _draggingItem;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _parentScrollRect = GetComponentInParent<ScrollRect>();
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerDownTime = Time.unscaledTime;
        }
        
        public void ReturnToOriginalParent()
        {
            if (_originalParent == null)
                return;

            transform.SetParent(_originalParent, false);
            transform.SetSiblingIndex(_originalIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            float heldTime = Time.unscaledTime - _pointerDownTime;

            // 짧게 드래그하면 스크롤로 넘김
            if (heldTime < longPressTime)
            {
                _routeToScroll = true;
                _parentScrollRect?.OnBeginDrag(eventData);
                return;
            }

            // 길게 누른 뒤 드래그하면 아이템 이동 시작
            _draggingItem = true;
            _originalParent = transform.parent;
            _originalIndex = transform.GetSiblingIndex();

            transform.SetParent(_rootCanvas.transform, true);
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_routeToScroll)
            {
                _parentScrollRect?.OnDrag(eventData);
                return;
            }

            if (!_draggingItem)
                return;

            _rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_routeToScroll)
            {
                _parentScrollRect?.OnEndDrag(eventData);
                _routeToScroll = false;
                return;
            }

            if (!_draggingItem)
                return;

            _draggingItem = false;
            _canvasGroup.blocksRaycasts = true;

            // 드롭 실패 시 원래 자리로 복귀
            if (transform.parent == _rootCanvas.transform)
            {
                transform.SetParent(_originalParent, false);
                transform.SetSiblingIndex(_originalIndex);
            }
        }
    }
}