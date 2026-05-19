using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Exp
{
    public class ExpTextBounceUI : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private TextMeshProUGUI targetText;

        [Header("Bounce")]
        [SerializeField] private float moveY = 25f;
        [SerializeField] private float scaleSize = 1.25f;
        [SerializeField] private float duration = 0.15f;
        
        [Header("Color")]
        [SerializeField] private Color bounceColor = Color.blue;

        private Vector2 _originPos;
        private Vector3 _originScale;
        private Color _originColor;
    
        private MotionHandle _positionHandle;
        private MotionHandle _scaleHandle;
        private MotionHandle _colorHandle;

        private void Awake()
        {
            if (target == null)
                target = transform as RectTransform;

            if (targetText == null)
                targetText = GetComponent<TextMeshProUGUI>();

            _originPos = target.anchoredPosition;
            _originScale = target.localScale;

            if (targetText != null)
                _originColor = targetText.color;
        }

        public void PlayBounce()
        {
            _positionHandle.TryCancel();
            _scaleHandle.TryCancel();
            _colorHandle.TryCancel();

            target.anchoredPosition = _originPos;
            target.localScale = _originScale;

            if (targetText != null)
                targetText.color = _originColor;

            _positionHandle = LMotion.Create(
                    _originPos,
                    _originPos + Vector2.up * moveY,
                    duration
                )
                .WithEase(Ease.OutCubic)
                .WithLoops(2, LoopType.Yoyo)
                .BindToAnchoredPosition(target);

            _scaleHandle = LMotion.Create(
                    _originScale,
                    _originScale * scaleSize,
                    duration
                )
                .WithEase(Ease.OutCubic)
                .WithLoops(2, LoopType.Yoyo)
                .BindToLocalScale(target);

            _colorHandle = LMotion.Create(
                    _originColor,
                    bounceColor,
                    duration
                )
                .WithEase(Ease.OutCubic)
                .WithLoops(2, LoopType.Yoyo)
                .Bind(color =>
                {
                    if (targetText != null)
                        targetText.color = color;
                });
        }

        private void OnDestroy()
        {
            _positionHandle.TryCancel();
            _scaleHandle.TryCancel();
            _colorHandle.TryCancel();
        }
    }
}