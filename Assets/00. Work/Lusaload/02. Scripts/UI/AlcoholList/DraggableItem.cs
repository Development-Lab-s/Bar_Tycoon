using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts
{
    // 롱 프레스를 감지해 짧은 드래그는 스크롤로, 긴 드래그는 아이템 이동으로 분기하는 컴포넌트
    public class DraggableItem : MonoBehaviour,  IPointerDownHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float longPressTime = 0.2f; // 이 시간 이상 누른 뒤 드래그해야 아이템 이동 모드 진입

        private ScrollRect _parentScrollRect; // 짧은 드래그를 위임할 부모 스크롤
        private Canvas _rootCanvas;           // 드래그 중 아이템을 최상위로 올리기 위한 루트 캔버스
        private CanvasGroup _canvasGroup;     // 드래그 중 레이캐스트 차단 제어
        private RectTransform _rectTransform; // 드래그 위치 갱신에 사용

        private Transform _originalParent; // 드롭 실패 시 복귀할 원래 부모
        private int _originalIndex;        // 드롭 실패 시 복귀할 원래 인덱스

        private float _pointerDownTime; // 포인터 누른 시각 (롱 프레스 판별용)
        private bool _routeToScroll;    // true면 이번 드래그를 스크롤에 위임
        private bool _draggingItem;     // true면 아이템 이동 드래그 진행 중
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _parentScrollRect = GetComponentInParent<ScrollRect>();
            _rootCanvas = GetComponentInParent<Canvas>();
        }

        // 포인터가 눌린 시각을 기록해 롱 프레스 여부를 판별할 기준점으로 사용
        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerDownTime = Time.unscaledTime;
        }

        // 드롭 대상이 없거나 드롭이 실패한 경우 아이템을 원래 위치로 되돌림
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