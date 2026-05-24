using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using System;
using UnityEngine;

namespace BBJ.UI
{
    public class DialogueBubbleUI : MonoBehaviour, IAgentUI
    {
        [SerializeField] private TMP_Text    _label;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float       _displayDuration = 1.5f;
        [SerializeField] private float       _animDuration    = 0.2f;

        public bool IsOpen { get; private set; }

        private void Start()
        {
            gameObject.SetActive(false);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        public void SetText(string text)
        {
            if (_label != null) _label.text = text;
        }

        public async UniTask OpenAsync()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            if (_canvasGroup != null)
                await LMotion.Create(0f, 1f, _animDuration)
                    .WithEase(Ease.OutCubic)
                    .BindToAlpha(_canvasGroup)
                    .AddTo(this);
            await UniTask.Delay(TimeSpan.FromSeconds(_displayDuration), cancellationToken: destroyCancellationToken);
        }

        public async UniTask CloseAsync()
        {
            if (_canvasGroup != null)
                await LMotion.Create(1f, 0f, _animDuration)
                    .WithEase(Ease.InCubic)
                    .BindToAlpha(_canvasGroup)
                    .AddTo(this);
            gameObject.SetActive(false);
            IsOpen = false;
        }
    }
}
