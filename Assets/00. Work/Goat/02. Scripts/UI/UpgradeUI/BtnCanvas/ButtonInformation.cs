using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.SaveCode;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas.ButtonDatas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas
{
    [RequireComponent(typeof(RectTransform), typeof(Image), typeof(Button))]
    public class ButtonInformation : MonoBehaviour
    {
        [field: SerializeField] public ButtonType ButtonType { get; private set; }
        
        [Header("Detail")]
        [SerializeField] private Sprite activeSprite;
        [SerializeField] private Sprite noActiveSprite;
        [SerializeField] private Image lockImage;
        [SerializeField] private float activePosYUp = 20;
        [SerializeField] private float activeFontSizeAdd = 10;
        [SerializeField] private float activeWidthSizeUp = 50;
        
        public bool canOpen { get; private set; }
        
        private RectTransform _rectTransform;
        private Image _image;
        private TextMeshProUGUI _text;
        private Button _button;
        
        private Vector2 _originPos;
        private Vector2 _originWidth;
        private float _originFontSize;
        
        public event Action<ButtonType> OnClickBtn;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _text = GetComponentInChildren<TextMeshProUGUI>();
            _image = GetComponent<Image>();
            _button = GetComponent<Button>();

            _originPos = _rectTransform.anchoredPosition;
            _originWidth = _rectTransform.sizeDelta;
            _originFontSize = _text.fontSize;
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClickBtn);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClickBtn);
        }

        private void HandleClickBtn()
        {
            if (!canOpen)
                return;
            
            OnClickBtn?.Invoke(ButtonType);
        }
        public void SetOpen(bool canOpen)
        {
            this.canOpen = canOpen;

            if (canOpen)
                NoActive();
            else
                Lock();
        }

        public void Active()
        {
            if (!canOpen)
            {
                Lock();
                return;
            }
            Setting(activePosYUp,activeWidthSizeUp, activeSprite, activeFontSizeAdd, true);
        }

        public void NoActive()
        {
            if (!canOpen)
            {
                Lock();
                return;
            }
            Setting(0,0,noActiveSprite,0, true);
        }

        private void Lock()
        {
            Setting(0,0,noActiveSprite,0,  false, true);
        }

        private void Setting(float posYUp, float widthSizeUp, Sprite imageSprite, float fontSizeAdd, bool buttonInteract, bool lockImageSetting = false)
        {
            _rectTransform.anchoredPosition = new Vector2(_originPos.x, _originPos.y + posYUp);
            _rectTransform.sizeDelta = new  Vector2(_originWidth.x + widthSizeUp, _originWidth.y);
            _image.sprite = imageSprite;
            _text.fontSize = _originFontSize + fontSizeAdd;
            _button.interactable = buttonInteract;
            lockImage.gameObject.SetActive(lockImageSetting);
        }
    }
}