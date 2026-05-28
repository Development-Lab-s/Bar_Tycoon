using _00._Work._Resources._02._Scripts.Agents.Players;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems;
using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using _00._Work.Goat._02._Scripts.Coin.CoinDatas;
using _00._Work.PCM._02._Scripts;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using System.Diagnostics.Tracing;
using _00._Work.Goat._02._Scripts.Coin;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using Systems;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class likeabilityController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private CharItemSO _itemSO;
    [SerializeField] private CoinManager coinManager;
    [SerializeField] private CharacterRegisterSO _characterRegister;
    [SerializeField] private TextMeshProUGUI _priceText;

    [Header("Setting")]
    [SerializeField] private LayerMask charact;

    [Header("Events")]
    [SerializeField] private EventChannelSO soundChannel;
    public UnityEvent errorMessage;
    public UnityEvent targetMessage;

    private Image _image;
    private GameObject dragInstance;
    private RectTransform dragRectTransform;
    private Canvas mainCanvas;

    private const float CostMultiplier = 0.2f;

    private void OnEnable()
    {
        _image = GetComponent<Image>();
        mainCanvas = GetComponentInParent<Canvas>();
        UpdatePriceText();
        LoadItem();
    }

    private int GetCurrentLevel()
    {
        if (_characterRegister == null || _itemSO == null) return 1;
        return _characterRegister.GetCharacterByName(_itemSO.Ownercharacter)?.CurrentLevel ?? 1;
    }

    private long GetCurrentCost()
    {
        return (long)(_itemSO.Price * (1f + GetCurrentLevel() * CostMultiplier));
    }

    private void UpdatePriceText()
    {
        if (_priceText == null || _itemSO == null) return;
        _priceText.text = $"{GetCurrentCost()}G";
    }

    private void LoadItem()
    {
        if (!SaveManager.IsSaveFile($"{_itemSO.ItemName}.save", "Items")) return;

        CharItemSaveData saveData = (CharItemSaveData)SaveManager.Load(
            typeof(CharItemSaveData),
            $"{_itemSO.ItemName}.save",
            "Items");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        long cost = GetCurrentCost();
        if (coinManager.CurrentCoin < cost)
        {
            errorMessage?.Invoke();
            return;
        }

        if (_image.sprite == null) return;

        dragInstance = new GameObject("DragIcon_Clone");
        dragInstance.transform.SetParent(mainCanvas.transform, false);
        dragInstance.transform.SetAsLastSibling();

        Image dragImg = dragInstance.AddComponent<Image>();
        dragImg.sprite = _image.sprite;
        dragImg.rectTransform.sizeDelta = _image.rectTransform.sizeDelta;
        dragImg.raycastTarget = false;

        dragRectTransform = dragInstance.GetComponent<RectTransform>();
        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragRectTransform != null)
            UpdateDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        long cost = GetCurrentCost();
        if (coinManager.CurrentCoin < cost)
        {
            Destroy(dragInstance);
            return;
        }

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(eventData.position);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 100, charact);

        if (hit.collider != null)
        {
            if (hit.collider.gameObject.TryGetComponent<ILikeabillty>(out ILikeabillty pychar))
            {
                if (_itemSO.CharacterEnum != pychar.characterEnum)
                {
                    targetMessage?.Invoke();
                    Destroy(dragInstance);
                    return;
                }
                soundChannel.RaiseEvent(new PlaySoundEvent((SfxSounds)13, Vector3.zero, SoundChannelId.None));
                coinManager.TryUseCoin(cost);
                pychar.OnLike.Invoke(_itemSO.LikePlus);
                UpdatePriceText();
            }
        }

        if (dragInstance != null)
            Destroy(dragInstance);
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);
        dragRectTransform.localPosition = localPoint;
    }

    public void OnDrop(PointerEventData eventData) { }
}
