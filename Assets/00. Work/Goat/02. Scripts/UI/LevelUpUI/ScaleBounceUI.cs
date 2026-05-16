using System;
using System.Collections;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    public class ScaleBounceUI : MonoBehaviour
    {
        [SerializeField] private RectTransform target;

        [Header("Scale Bounce")]
        [SerializeField] private float scaleSize = 1.15f;
        [SerializeField] private float duration = 0.12f;
        [SerializeField] private float waitTime = 0.3f;

        private Vector3 _originScale;
        private MotionHandle _scaleHandle;

        private void Awake()
        {
            if (target == null)
                target = transform as RectTransform;

            _originScale = target.localScale;
        }

        private void OnEnable()
        {
            StartCoroutine(PlayBounceCoroutine());
        }

        private IEnumerator PlayBounceCoroutine()
        {
            yield return new WaitForSeconds(waitTime);
            PlayBounce();
        }

        public void PlayBounce()
        {
            _scaleHandle.TryCancel();

            target.localScale = _originScale;

            _scaleHandle = LMotion.Create(
                    _originScale,
                    _originScale * scaleSize,
                    duration
                )
                .WithEase(Ease.OutQuad)
                .WithLoops(2, LoopType.Yoyo)
                .BindToLocalScale(target);
        }

        private void OnDestroy()
        {
            _scaleHandle.TryCancel();
        }
    }
}