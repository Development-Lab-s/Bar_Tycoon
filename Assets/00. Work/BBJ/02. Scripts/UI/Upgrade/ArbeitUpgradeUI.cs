using System;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace BBJ.UI.Upgrade
{
    public class ArbeitUpgradeUI : MonoBehaviour
    {
        [Serializable]
        private class CategoryContent
        {
            public ArbeitEnum category;
            public GameObject contentObject;
        }

        [SerializeField] private ArbeitCategoryButtonUI[] _categoryButtons;
        [SerializeField] private ArbeitEnum               _defaultCategory = ArbeitEnum.SERVING;
        [SerializeField] private CategoryContent[]        _categoryContents;

        [SerializeField] private float _moveDuration  = 0.2f;
        [SerializeField] private float _selectedMoveY = 18f;
        [SerializeField] private float _normalWidth   = 250f;
        [SerializeField] private float _selectedWidth = 300f;
        [SerializeField] private Ease  _ease          = Ease.OutCubic;

        private ArbeitEnum             _currentCategory;
        private ArbeitCategoryButtonUI _currentButton;

        private readonly Dictionary<RectTransform, float> _originalY = new();

        private void Start() => SetDefaultCategory();

        private void SetDefaultCategory()
        {
            foreach (var btn in _categoryButtons)
            {
                bool selected = btn.Category == _defaultCategory;
                RegisterOriginalY(btn.Rect);
                btn.SetSelected(selected);
                ApplyButtonState(btn.Rect, selected);

                if (selected)
                {
                    _currentCategory = btn.Category;
                    _currentButton   = btn;
                    ChangeContent(btn.Category);
                }
            }
        }

        public void OnClickCategory(ArbeitCategoryButtonUI button)
        {
            if (_currentCategory == button.Category) return;

            ChangeContent(button.Category);

            if (_currentButton != null)
            {
                _currentButton.SetSelected(false);
                AnimateButton(_currentButton.Rect, false);
            }

            _currentCategory = button.Category;
            _currentButton   = button;
            _currentButton.SetSelected(true);
            AnimateButton(_currentButton.Rect, true);
        }

        private void ChangeContent(ArbeitEnum category)
        {
            foreach (var content in _categoryContents)
                if (content.contentObject != null)
                    content.contentObject.SetActive(content.category == category);
        }

        private void AnimateButton(RectTransform btn, bool selected)
        {
            RegisterOriginalY(btn);
            float targetY     = _originalY[btn] + (selected ? _selectedMoveY : 0f);
            float targetWidth = selected ? _selectedWidth : _normalWidth;

            LMotion.Create(btn.anchoredPosition.y, targetY, _moveDuration)
                .WithEase(_ease)
                .BindToAnchoredPositionY(btn)
                .AddTo(this);

            LMotion.Create(btn.sizeDelta.x, targetWidth, _moveDuration)
                .WithEase(_ease)
                .Bind(w => btn.sizeDelta = new Vector2(w, btn.sizeDelta.y))
                .AddTo(this);
        }

        private void ApplyButtonState(RectTransform btn, bool selected)
        {
            float targetY     = _originalY[btn] + (selected ? _selectedMoveY : 0f);
            float targetWidth = selected ? _selectedWidth : _normalWidth;
            btn.anchoredPosition = new Vector2(btn.anchoredPosition.x, targetY);
            btn.sizeDelta        = new Vector2(targetWidth, btn.sizeDelta.y);
        }

        private void RegisterOriginalY(RectTransform btn)
        {
            if (!_originalY.ContainsKey(btn))
                _originalY[btn] = btn.anchoredPosition.y;
        }
    }
}
