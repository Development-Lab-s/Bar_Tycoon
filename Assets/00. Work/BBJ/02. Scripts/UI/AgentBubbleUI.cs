using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI
{
    public class AgentBubbleUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _animDuration = 0.15f;
        [SerializeField] private Image _backgroundImage;
        public Image BackgroundImage => _backgroundImage;
        public bool IsOpen { get; private set; }

        private void Start()
        {
            if (_canvasGroup == null) return;

            _canvasGroup.gameObject.SetActive(false);
            _canvasGroup.alpha = 0f;
        }

        public async UniTask OpenAsync()
        {
            if (_canvasGroup == null) return;
            _canvasGroup.gameObject.SetActive(true);
            IsOpen = true;
            await LMotion.Create(0f, 1f, _animDuration)
                .WithEase(Ease.OutCubic)
                .BindToAlpha(_canvasGroup)
                .AddTo(this);
        }

        public async UniTask CloseAsync()
        {
            if (_canvasGroup == null) return;
            IsOpen = false;
            await LMotion.Create(1f, 0f, _animDuration)
                .WithEase(Ease.InCubic)
                .BindToAlpha(_canvasGroup)
                .AddTo(this);
            _canvasGroup.gameObject.SetActive(false);
        }
    }
}
