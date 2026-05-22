using System;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class ArbeitUpgradeUI : MonoBehaviour
{
    [Serializable]
    private class CategoryContent
    {
        public ArbeitEnum category;
        public GameObject contentObject;
    }

    [Header("Category")]
    [SerializeField] private ArbeitCategoryButton[] categoryButtons;
    [SerializeField] private ArbeitEnum defaultCategory = ArbeitEnum.SERVING;

    [Header("Content")]
    [SerializeField] private CategoryContent[] categoryContents;

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
            bool selected = button.Category == defaultCategory;

            RegisterOriginalY(button.RectTransform);
            button.SetSelected(selected);
            SetButtonState(button.RectTransform, selected);

            if (selected)
            {
                currentCategory = button.Category;
                currentButton = button;
                ChangeUpgradeList(button.Category);
            }
        }
    }

    public void OnClickCategory(ArbeitCategoryButton button)
    {
        if (currentCategory == button.Category)
            return;

        ChangeUpgradeList(button.Category);

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

    private void ChangeUpgradeList(ArbeitEnum category)
    {
        Debug.Log($"카테고리 변경 : {category}");

        foreach (CategoryContent content in categoryContents)
        {
            bool active = content.category == category;

            if (content.contentObject != null)
                content.contentObject.SetActive(active);
        }
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
        float targetY = originalButtonY[button] + (selected ? selectedMoveY : 0f);
        float targetWidth = selected ? selectedWidth : normalWidth;

        button.anchoredPosition = new Vector2(button.anchoredPosition.x, targetY);
        button.sizeDelta = new Vector2(targetWidth, button.sizeDelta.y);
    }

    private void RegisterOriginalY(RectTransform button)
    {
        if (!originalButtonY.ContainsKey(button))
            originalButtonY[button] = button.anchoredPosition.y;
    }
}