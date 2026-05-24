using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    public class ArbeitUpgradeUIFix : MonoBehaviour
    {
            [Header("Category")]
    [SerializeField] private ArbeitCategoryButton[] categoryButtons;
    [SerializeField] private ArbeitEnum defaultCategory = ArbeitEnum.SERVING;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float selectedMoveY = 18f;
    [SerializeField] private float normalWidth = 250f;
    [SerializeField] private float selectedWidth = 300f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    private ArbeitEnum currentCategory;
    private ArbeitCategoryButton currentButton;

    private readonly Dictionary<RectTransform, float> originalButtonY = new();

    private void Start()
    {
        SetDefaultCategory();
    }

    private void SetDefaultCategory()
    {
        foreach (ArbeitCategoryButton button in categoryButtons)
        {
            RegisterOriginalY(button.RectTransform);

            bool selected = button.Category == defaultCategory;

            button.SetSelected(selected);
            SetButtonState(button.RectTransform, selected);

            if (selected)
            {
                currentCategory = button.Category;
                currentButton = button;
            }
        }
    }

    public void OnClickCategory(ArbeitCategoryButton button)
    {
        if (button == null)
            return;

        if (currentCategory == button.Category)
            return;

        if (currentButton != null)
        {
            currentButton.SetSelected(false);
            AnimateButton(currentButton.RectTransform, false);
        }

        currentCategory = button.Category;
        currentButton = button;

        currentButton.SetSelected(true);
        AnimateButton(currentButton.RectTransform, true);
    }

    private void AnimateButton(RectTransform button, bool selected)
    {
        RegisterOriginalY(button);

        float targetY = originalButtonY[button] + (selected ? selectedMoveY : 0f);
        float targetWidth = selected ? selectedWidth : normalWidth;

        LMotion.Create(button.anchoredPosition.y, targetY, moveDuration)
            .WithEase(ease)
            .BindToAnchoredPositionY(button)
            .AddTo(this);

        LMotion.Create(button.sizeDelta.x, targetWidth, moveDuration)
            .WithEase(ease)
            .Bind(width =>
            {
                Vector2 size = button.sizeDelta;
                size.x = width;
                button.sizeDelta = size;
            })
            .AddTo(this);
    }

    private void SetButtonState(RectTransform button, bool selected)
    {
        RegisterOriginalY(button);

        float targetY = originalButtonY[button] + (selected ? selectedMoveY : 0f);
        float targetWidth = selected ? selectedWidth : normalWidth;

        button.anchoredPosition = new Vector2(button.anchoredPosition.x, targetY);
        button.sizeDelta = new Vector2(targetWidth, button.sizeDelta.y);
    }

    private void RegisterOriginalY(RectTransform button)
    {
        if (button == null)
            return;

        if (!originalButtonY.ContainsKey(button))
            originalButtonY[button] = button.anchoredPosition.y;
    }
    }
}