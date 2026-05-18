using UnityEngine;
using UnityEngine.UI;

public class RatingBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;   // Fill 방식 Image
    [SerializeField] private Color fillColor;   // 각 맛마다 다른 색

    private void Awake()
    {
        fillImage.color = fillColor;
    }

    // 0~5 값을 0~1로 변환해서 fillAmount에 적용
    public void SetRating(int value)
    {
        Debug.Log(value / 5f);
        fillImage.fillAmount = Mathf.Clamp01(value / 5f);
    }
}