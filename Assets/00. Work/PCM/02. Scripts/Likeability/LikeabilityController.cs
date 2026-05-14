using _00._Work._Resources._02._Scripts.Agents.Players;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems;
using _00._Work.PCM._02._Scripts;
using Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 1. IDragHandler를 반드시 상속받아야 합니다.
public class likeabilityController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    Vector3 DefaultPos;
    Image _image;
    public void Awake()
    {
        _image = GetComponent<Image>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        DefaultPos = this.transform.position;
        _image.raycastTarget = false;
    }
    public void OnDrag(PointerEventData eventData)
    {
        this.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        Debug.Log(mousePos);
        RaycastHit2D hit = Physics2D.Raycast(mousePos,Vector2.zero);
        if (hit.collider != null)
        {
            if (hit.collider.gameObject.TryGetComponent<IPlayer>(out IPlayer iq))
            {
                Debug.Log(iq.charLike.likeability);
                iq.ChatOpen?.Invoke();
            }
        }
        this.transform.position = DefaultPos;
        _image.raycastTarget = true;
    }
    public void OnDrop(PointerEventData eventData) { }
}