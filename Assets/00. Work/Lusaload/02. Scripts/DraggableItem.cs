using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts
{
    public class DraggableItem : MonoBehaviour,  IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float longPressTime = 0.2f;

        private ScrollRect parentScrollRect;
        private Canvas rootCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;

        private Transform originalParent;
        private int originalIndex;

        private float pointerDownTime;
        private bool pointerHeld;
        private bool routeToScroll;
        private bool draggingItem;



        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            parentScrollRect = GetComponentInParent<ScrollRect>();
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerHeld = true;
            pointerDownTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerHeld = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            float heldTime = Time.unscaledTime - pointerDownTime;

            // 짧게 드래그하면 스크롤로 넘김
            if (heldTime < longPressTime)
            {
                routeToScroll = true;
                parentScrollRect?.OnBeginDrag(eventData);
                return;
            }

            // 길게 누른 뒤 드래그하면 아이템 이동 시작
            draggingItem = true;
            originalParent = transform.parent;
            originalIndex = transform.GetSiblingIndex();

            transform.SetParent(rootCanvas.transform, true);
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (routeToScroll)
            {
                parentScrollRect?.OnDrag(eventData);
                return;
            }

            if (!draggingItem)
                return;

            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (routeToScroll)
            {
                parentScrollRect?.OnEndDrag(eventData);
                routeToScroll = false;
                return;
            }

            if (!draggingItem)
                return;

            draggingItem = false;
            canvasGroup.blocksRaycasts = true;

            // 드롭 실패 시 원래 자리로 복귀
            if (transform.parent == rootCanvas.transform)
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalIndex);
            }
        }
    }
}