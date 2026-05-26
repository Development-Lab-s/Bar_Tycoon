using System;
using LitMotion;
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
        
        [Header("Active Setting")]
        [SerializeField] private float activePosYUp = 20f;
        [SerializeField] private float activeFontSizeAdd = 10f;
        [SerializeField] private float activeWidthSizeUp = 50f;

        [Header("Motion")]
        [SerializeField] private float moveDuration = 0.2f;
        [SerializeField] private Ease ease = Ease.OutCubic;
        
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
            Initialize();
        }
        
        private bool _isInitialized;

        private void Initialize()
        {
            if (_isInitialized)
                return;

            _rectTransform = GetComponent<RectTransform>();
            _text = GetComponentInChildren<TextMeshProUGUI>();
            _image = GetComponent<Image>();
            _button = GetComponent<Button>();

            _originPos = _rectTransform.anchoredPosition;
            _originWidth = _rectTransform.sizeDelta;

            if (_text != null)
                _originFontSize = _text.fontSize;

            _isInitialized = true;
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
            Initialize();
            this.canOpen = canOpen;

            if (canOpen)
                NoActive(false);
            else
                Lock(false);
        }

        public void Active()
        {
            if (!canOpen)
            {
                Lock();
                return;
            }

            SetVisual(activeSprite, true, false);
            AnimateSetting(activePosYUp, activeWidthSizeUp, activeFontSizeAdd);
        }
        
        public void NoActive()
        {
            if (!canOpen)
            {
                Lock();
                return;
            }

            SetVisual(noActiveSprite, true, false);
            AnimateSetting(0f, 0f, 0f);
        }

        public void NoActive(bool useMotion)
        {
            if (!canOpen)
            {
                Lock(useMotion);
                return;
            }

            SetVisual(noActiveSprite, true, false);

            if (useMotion)
                AnimateSetting(0f, 0f, 0f);
            else
                SetSettingImmediately(0f, 0f, 0f);

        }
        
        private void Lock()
        {
            Lock(true);
        }

        private void Lock(bool useMotion)
        {
            SetVisual(noActiveSprite, false, true);

            if (useMotion)
                AnimateSetting(0f, 0f, 0f);
            else
                SetSettingImmediately(0f, 0f, 0f);
        }

        private void SetVisual(Sprite sprite, bool buttonInteract, bool lockImageSetting)
        {
            if (_image != null)
                _image.sprite = sprite;

            if (_button != null)
                _button.interactable = buttonInteract;

            if (lockImage != null)
                lockImage.gameObject.SetActive(lockImageSetting);
        }

        private void AnimateSetting(float posYUp, float widthSizeUp, float fontSizeAdd)
        {
            Vector2 targetPos = new Vector2(_originPos.x, _originPos.y + posYUp);
            Vector2 targetSize = new Vector2(_originWidth.x + widthSizeUp, _originWidth.y);
            float targetFontSize = _originFontSize + fontSizeAdd;

            LMotion.Create(_rectTransform.anchoredPosition, targetPos, moveDuration)
                .WithEase(ease)
                .Bind(value => _rectTransform.anchoredPosition = value)
                .AddTo(this);

            LMotion.Create(_rectTransform.sizeDelta, targetSize, moveDuration)
                .WithEase(ease)
                .Bind(value => _rectTransform.sizeDelta = value)
                .AddTo(this);

            if (_text != null)
            {
                LMotion.Create(_text.fontSize, targetFontSize, moveDuration)
                    .WithEase(ease)
                    .Bind(value => _text.fontSize = value)
                    .AddTo(this);
            }
        }

        private void SetSettingImmediately(float posYUp, float widthSizeUp, float fontSizeAdd)
        {
            Initialize();
            _rectTransform.anchoredPosition = new Vector2(_originPos.x, _originPos.y + posYUp);
            _rectTransform.sizeDelta = new Vector2(_originWidth.x + widthSizeUp, _originWidth.y);

            if (_text != null)
                _text.fontSize = _originFontSize + fontSizeAdd;
        }
    }
}