using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class UIPopup : MonoBehaviour, IPopup
{
    [SerializeField] private float duration = 0.3f;
    private RectTransform _target;

    public bool IsAniming { get; private set; }

    private void Awake()
    {
        _target = GetComponent<RectTransform>();
    }

    private void OnEnable() => PlaySlideUp();

    public async void OnClose()
    {
        if (IsAniming)
            return;

        IsAniming = true;

        var tween = LMotion.Create(
                new Vector2(_target.anchoredPosition.x, 0f),
                new Vector2(_target.anchoredPosition.x, -Screen.height * 2),
                duration)
            .WithEase(Ease.OutCubic)
            .BindToAnchoredPosition(_target)
            .AddTo(this);

        await tween;

        IsAniming = false;
        UIManager.Instance.ClosePopup();
    }

    public void OnClickClose() => OnClose();

    private async void PlaySlideUp()
    {
        IsAniming = true;
        
        var x = _target.anchoredPosition.x;
        _target.anchoredPosition = new Vector2(x, -Screen.height);

        var tween = LMotion.Create(
                new Vector2(x, -Screen.height),
                new Vector2(x, 0f),
                duration)
            .WithEase(Ease.OutCubic)
            .BindToAnchoredPosition(_target);

        await tween;
        IsAniming = false;
    }
}