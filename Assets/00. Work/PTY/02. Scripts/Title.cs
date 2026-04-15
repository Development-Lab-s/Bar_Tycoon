using System.Collections;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class Title : MonoBehaviour
{
    [SerializeField] private Image[] titleElements;
    [SerializeField] private TextMeshProUGUI touchAnywhere;
    [SerializeField] private Image background;
    [SerializeField] private Image cover;
    [SerializeField] private GameObject mainUI;
    
    private float[] durations = { // 타이틀 로고 각 텍스트 애니메이션 속도
        0.1f, 0.1f, 0.1f, 0.1f,
        0.5f,
        0.1f, 0.1f, 0.1f,
        0.2f
    };
    
    private float[] sizeMultipliers = { // 타이틀 로고 각 텍스트 애니메이션 역동적인 정도
        0.8f, 0.8f, 0.8f, 0.8f,
        0.4f,
        0.8f, 0.8f, 0.8f,
        0.6f
    };

    private Color32 CoverColor = new Color32(255, 230, 210, 255);
    private Color32 invisibleCoverColor = new Color32(255, 230, 210, 0);
    
    private string text;
    private bool isAniming = true;
    private bool isCovering = false;
    private bool isBlink = true;
    private bool isTitle = true;
    
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
        mainUI.SetActive(false);
        StartCoroutine(GameStartEffect());
    }

    private IEnumerator GameStartEffect()
    {
        EndCover();
        yield return new WaitForSeconds(1f);
        TitleAppearAnim();
    }

    private void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began && !isAniming && isTitle)
            {
                StartCover();
                isTitle = false;
            }
        }
    }

    async void StartCover() // 타이틀 화면 전환 시 화면 덮어주는 커버 알파갚 1
    {
        if (isCovering) return;
        isCovering = true;
        cover.gameObject.SetActive(true);
        var coverTween = LMotion.Create(invisibleCoverColor, CoverColor, 0.8f)
            .WithEase(Ease.OutQuad)
            .BindToColor(cover)
            .ToAwaitable();
        await System.Threading.Tasks.Task.Delay(1000);
            
        background.gameObject.SetActive(false);
        mainUI.SetActive(true);
        titleElements[0].transform.parent.gameObject.SetActive(false);

        isBlink = false;
        touchAnywhere.gameObject.SetActive(false);
        
        EndCover();
    }

    async void EndCover() // 타이틀 화면 전환 시 화면 덮어주는 커버 알파갚 0
    {
        var coverTween = LMotion.Create(CoverColor, invisibleCoverColor, 0.7f)
            .WithEase(Ease.InQuad)
            .BindToColor(cover)
            .ToAwaitable();
        
        await coverTween;
        
        cover.gameObject.SetActive(false);
        isCovering = false;
    }
    
    async void TitleAppearAnim() // 로고 텍스트 애니메이션
    {
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

    async void TypeWriter() // 계속하려면 화면을 클릭하세요 UI 타이핑 이펙트
    {
        touchAnywhere.text = string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            touchAnywhere.text += text[i];
            await System.Threading.Tasks.Task.Delay(20);
        }
        
        StartBlink();
        isAniming = false;
    }

    async void StartBlink() // 계속하려면 화면을 클릭하세요 UI 깜빡임
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

    void SetAlpha(float a) // 계속하려면 화면을 클릭하세요 UI 깜빡임 알파갚 변환
    {
        var c = touchAnywhere.color;
        c.a = a;
        touchAnywhere.color = c;
    }
}
