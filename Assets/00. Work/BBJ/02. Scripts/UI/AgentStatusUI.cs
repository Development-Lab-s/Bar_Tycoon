using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI
{
    public class AgentStatusUI : MonoBehaviour, IAgentUI
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _animDuration = 0.15f;
        public Image Icon => _icon;
        public bool IsOpen { get; private set; }


        private void Start()
        {
            _canvasGroup.gameObject.SetActive(false);
            IsOpen = false;
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        public async UniTask OpenAsync()
        {
            if (_canvasGroup == null) return;

            SetActive(true);
            await LMotion.Create(0f, 1f, _animDuration)
                .WithEase(Ease.OutCubic)
                .BindToAlpha(_canvasGroup)
                .AddTo(this);
        }

        public async UniTask CloseAsync()
        {
            if (_canvasGroup == null) return;

            await LMotion.Create(1f, 0f, _animDuration)
                .WithEase(Ease.InCubic)
                .BindToAlpha(_canvasGroup)
                .AddTo(this);
            SetActive(false);
        }
        public void ToggleIcon()
        {
            _label.gameObject.SetActive(false);
            _icon.gameObject.SetActive(true);
        }
        public void ToggleText()
        {
            _icon.gameObject.SetActive(false);
            _label.gameObject.SetActive(true);
        }
        public void ToggleText(string text)
        {
            SetText(text);
            ToggleText();
        }
        public void SetText(string text) => _label.text = text;
        private void SetActive(bool isActive)
        {
            if (this == null || _canvasGroup == null) return;
            IsOpen = isActive;
            _canvasGroup.gameObject.SetActive(isActive);
        }
    }
}
