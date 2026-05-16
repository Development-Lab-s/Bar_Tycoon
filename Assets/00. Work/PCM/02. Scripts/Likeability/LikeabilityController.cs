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
    private Image _image;

    // 드래그 중에 마우스를 따라다닐 임시 복사본 오브젝트
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
        if (_image.sprite == null) return;

        dragInstance = new GameObject("DragIcon_Clone");
        dragInstance.transform.SetParent(mainCanvas.transform, false);
        dragInstance.transform.SetAsLastSibling(); 

        Image dragImg = dragInstance.AddComponent<Image>();
        dragImg.sprite = _image.sprite;
        dragImg.rectTransform.sizeDelta = _image.rectTransform.sizeDelta;

        // 4. 가짜 이미지는 레이캐스트를 꺼서, 마우스를 놓았을 때 플레이어 레이가 막히지 않게 함
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
        if (dragInstance == null) Debug.Log("샤갈") ;
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