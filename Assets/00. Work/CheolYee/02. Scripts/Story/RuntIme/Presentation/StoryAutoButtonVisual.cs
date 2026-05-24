using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation
{
    public sealed class StoryAutoButtonVisual : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private RectTransform spinTarget;
        [SerializeField] private Graphic[] colorTarget;
        [SerializeField] private Color activeColor = Color.yellow;
        [SerializeField] private float spinSpeed = 360f;

        private bool _isSpinning;
        private Color _inactiveColor;

        private void Awake()
        {
            _inactiveColor = colorTarget[0].color;
            
            if (toggle != null)
                toggle.onValueChanged.AddListener(OnToggle);
        }

        private void OnDestroy()
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnToggle);
        }

        private void Update()
        {
            if (!_isSpinning || spinTarget == null) return;
            spinTarget.Rotate(0f, 0f, -spinSpeed * Time.unscaledDeltaTime);
        }

        private void OnToggle(bool isOn)
        {
            Debug.Log(isOn);
            _isSpinning = isOn;

            if (!isOn && spinTarget != null)
                spinTarget.localEulerAngles = Vector3.zero;

            if (colorTarget != null)
                foreach (var target in colorTarget)
                    target.color = isOn ? activeColor : _inactiveColor;
        }
    }
}
