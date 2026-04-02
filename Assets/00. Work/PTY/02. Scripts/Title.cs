using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class Title : MonoBehaviour
{
    public Image[] titleElements;
    public TextMeshProUGUI touchAnywhere;
    public Image background;
    public Image cover;
    
    private float[] durations = {
        0.1f, 0.1f, 0.1f, 0.1f,
        0.5f,
        0.1f, 0.1f, 0.1f,
        0.2f
    };
    
    private float[] sizeMultipliers = {
        0.8f, 0.8f, 0.8f, 0.8f,
        0.4f,
        0.8f, 0.8f, 0.8f,
        0.6f
    };

    private string text;
    private bool isOnTitle = true;
    private bool isBlink = true;
    private bool isAnimable = true;
    
    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }
    
    private void Start()
    {
        for(int i = 0; i < titleElements.Length; i++)
            titleElements[i].color = new Color(1, 1, 1, 0);
        text = touchAnywhere.text;
        touchAnywhere.text = string.Empty;
        
        TitleAppearAnim();
    }

    private void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                StartCover();
            }
        }
    }

    async void StartCover()
    {
        if (!isAnimable || !isOnTitle) return;
        isAnimable = false;
        
        var coverTween = LMotion.Create(new Color(1, 0.8941177f, 0.7686275f, 0), Color.bisque, 0.4f)
            .WithEase(Ease.OutQuad)
            .BindToColor(cover)
            .ToAwaitable();
        await System.Threading.Tasks.Task.Delay(1000);
        background.color = new Color(0.5f, 0.5f, 0.5f, 0);
        background.gameObject.SetActive(false);
        for(int i = 0; i < titleElements.Length; i++)
            titleElements[i].gameObject.SetActive(false);
        touchAnywhere.gameObject.SetActive(false);
        EndCover();
    }

    async void EndCover()
    {
        var coverTween = LMotion.Create(Color.bisque, new Color(1, 0.8941177f, 0.7686275f, 0), 0.7f)
            .WithEase(Ease.InQuad)
            .BindToColor(cover)
            .ToAwaitable();
        
        await coverTween;
        cover.gameObject.SetActive(false);
        
        isAnimable = true;
        isOnTitle = false;
    }
    
    async void TitleAppearAnim()
    {
        if (!isAnimable) return;
        isAnimable = false;
        
        for (int i = 0; i < titleElements.Length; i++)
        {
            var element = titleElements[i];
            float duration = durations[i];
            float sizeMultiplier = sizeMultipliers[i];

            var colorTween = LMotion.Create(new Color(1, 1, 1, 0), Color.white, duration)
                .WithEase(Ease.OutQuad)
                .BindToColor(element)
                .ToAwaitable();

            var scaleTween = LMotion.Create(Vector3.one * sizeMultiplier, Vector3.one, duration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(element.transform)
                .ToAwaitable();

            await colorTween;
            await scaleTween;
        }
        
        await System.Threading.Tasks.Task.Delay(200);
        TypeWriter();
    }

    async void TypeWriter()
    {
        touchAnywhere.text = string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            touchAnywhere.text += text[i];
            await System.Threading.Tasks.Task.Delay(20);
        }
        StartBlink();
        
        isAnimable = true;
    }

    async void StartBlink()
    {
        while (isBlink)
        {
            await LMotion.Create(1f, 0, 0.8f)
                .WithEase(Ease.InOutQuad)
                .Bind(SetAlpha)
                .ToAwaitable();
            await LMotion.Create(0, 1f, 0.8f)
                .WithEase(Ease.InOutQuad)
                .Bind(SetAlpha)
                .ToAwaitable();
        }
    }

    void SetAlpha(float a)
    {
        var c = touchAnywhere.color;
        c.a = a;
        touchAnywhere.color = c;
    }
}
