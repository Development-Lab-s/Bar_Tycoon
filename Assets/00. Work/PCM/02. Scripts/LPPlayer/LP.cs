using UnityEngine;
using UnityEngine.Rendering;
using LitMotion;

public class LP : MonoBehaviour
{
    [SerializeField] private Ease easeType;
    private RectTransform rect;
    private bool isRotate;
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        rect.anchoredPosition = Vector3.zero;
    }
    public void Active()
    {
        rect.anchoredPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        LMotion.Create(0, rect.rect.size.x * 0.75, 0.5f)
            .WithEase(easeType)
            .WithOnComplete
            (() =>
            {
                LMotion.Create(0f, 360f, 2f) // 2초 동안 360도 회전
                .WithLoops(-1, LoopType.Restart) 
                .Bind(angle =>
                {
                    rect.localRotation = Quaternion.Euler(0, 0, angle);
                });
            })
            .Bind(x =>
            {
                rect.anchoredPosition = new Vector2((float)x, rect.anchoredPosition.y);
            });
    }
}
