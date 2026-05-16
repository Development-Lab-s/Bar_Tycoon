using _00._Work._Resources._02._Scripts.Agents.Players;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems;
using _00._Work.PCM._02._Scripts;
using Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class likeabilityController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]private CharItemSO _itemSO; //Char은 매력이라는 뜻 ㅇㅇ
    private Image _image;
    private GameObject dragInstance;
    private RectTransform dragRectTransform;
    private Canvas mainCanvas;

    public void Awake()
    {
        _image = GetComponent<Image>();
        mainCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_image.sprite == null)
        {
            return;
        }
        ;

        dragInstance = new GameObject("DragIcon_Clone");
        dragInstance.transform.SetParent(mainCanvas.transform, false);
        dragInstance.transform.SetAsLastSibling(); 

        Image dragImg = dragInstance.AddComponent<Image>();
        dragImg.sprite = _image.sprite;
        dragImg.rectTransform.sizeDelta = _image.rectTransform.sizeDelta;

        dragImg.raycastTarget = false;

        dragRectTransform = dragInstance.GetComponent<RectTransform>();

        // 초기 위치 설정
        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragRectTransform != null)
        {
            UpdateDragPosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Debug.Log(mousePos);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            if (hit.collider.gameObject.TryGetComponent<IPlayer>(out IPlayer iq))
            {
                Debug.Log(iq.charLike.likeability);
                iq.ChatOpen?.Invoke();
            }
        }
        if (dragInstance != null)
        {
            Destroy(dragInstance);
        }
    }

    // 마우스의 스크린 좌표를 Canvas 내부 로컬 좌표로 정확하게 변환해주는 함수
    private void UpdateDragPosition(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        dragRectTransform.localPosition = localPoint;
    }

    public void OnDrop(PointerEventData eventData) { }
}