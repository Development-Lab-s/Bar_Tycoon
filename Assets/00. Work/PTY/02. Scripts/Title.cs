using System.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Title : MonoBehaviour
{
    public Image[] TitleElements;
    
    private float[] durations = {
        0.1f, 0.1f, 0.1f, 0.1f,
        0.7f,
        0.1f, 0.1f, 0.1f,
        0.2f
    };
    
    private float[] sizeMultipliers = {
        0.8f, 0.8f, 0.8f, 0.8f,
        0.4f,
        0.8f, 0.8f, 0.8f,
        0.6f
    };

    private void Start()
    {
        TitleAnim();
    }

    async void TitleAnim()
    {
        for (int i = 0; i < TitleElements.Length; i++)
        {
            var element = TitleElements[i];
            float duration = durations[i]; // 현재 순서에 맞는 속도 가져오기
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
    }
}
