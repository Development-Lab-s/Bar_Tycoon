using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.UI
{
    public class EatDurationUI : MonoBehaviour, IAgentUI, IModule
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private float  _animDuration = 0.15f;

        public bool IsOpen { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            if (_slider != null)
            {
                _slider.minValue = 0f;
                _slider.maxValue = 1f;
            }
            gameObject.SetActive(false);
        }

        public void SetPercent(float value)
        {
            if (_slider != null)
                _slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        public async UniTask OpenAsync()
        {
            SetPercent(0f);
            gameObject.SetActive(true);
            IsOpen = true;
            await LMotion.Create(Vector3.zero, Vector3.one, _animDuration)
                .WithEase(Ease.OutBack)
                .Bind(v => transform.localScale = v)
                .AddTo(this);
        }

        public async UniTask CloseAsync()
        {
            await LMotion.Create(Vector3.one, Vector3.zero, _animDuration)
                .WithEase(Ease.InCubic)
                .Bind(v => transform.localScale = v)
                .AddTo(this);
            gameObject.SetActive(false);
            IsOpen = false;
        }
    }
}
