using System;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using UnityEngine;

namespace BBJ.Modules
{
    public class SpineAgentRenderer : MonoBehaviour, IModule, IRenderer, IAnimatorTrigger, IHoverRenderer
    {
        [SerializeField] private GameObject[] _spineObjects;
        [SerializeField] private Renderer[] _skeletonMimics;
        [field: SerializeField] public float FacingDirection { get; private set; } = 1f;

        private Animator[] _animators;
        private int        _currentLayer;

        private MaterialPropertyBlock _mpb;

        //private float _currentOutlineFade = 0f;
        private float _currentTintFade = 0f;
        private float _tintValue = 0.1f;

        public event Action OnAnimationEnd;
        public event Action OnAttackTrigger;
        public event Action<bool> OnCounterStateChange;

        public void Initialize(ModuleOwner owner)
        {
            _mpb = new MaterialPropertyBlock();
            DisableHoverEffect();

            // [복구 완료] 애니메이터 캐싱 및 초기 레이어 설정 로직
            _animators = new Animator[_spineObjects.Length];
            for (int i = 0; i < _spineObjects.Length; i++)
            {
                _animators[i] = _spineObjects[i].GetComponent<Animator>();
            }

            _currentLayer = 0;
            for (int i = 0; i < _spineObjects.Length; i++)
            {
                _spineObjects[i].gameObject.SetActive(i == 0);

                if (_skeletonMimics != null && i < _skeletonMimics.Length)
                {
                    _skeletonMimics[i].gameObject.SetActive(i == 0);
                }
            }

            // [복구 완료] 애니메이션 이벤트 포워더 연결
            if (_spineObjects.Length > 0 && _spineObjects[0] != null)
            {
                var forwarder = _spineObjects[0].GetComponent<AnimationEventForwarder>()
                                ?? _spineObjects[0].gameObject.AddComponent<AnimationEventForwarder>();
                forwarder.Initialize(this);
            }
        }

        public void EnableHoverEffect()
        {
            //_currentOutlineFade = 1f;
            _currentTintFade    = _tintValue;
            ApplySSUToCurrentMimic();
        }

        public void DisableHoverEffect()
        {
            //_currentOutlineFade = 0f;
            _currentTintFade    = 0f;
            ApplySSUToCurrentMimic();
        }

        private void ApplySSUToCurrentMimic()
        {
            if (_skeletonMimics == null || _skeletonMimics.Length <= _currentLayer) return;

            Renderer targetMimic = _skeletonMimics[_currentLayer];
            if (targetMimic != null && _mpb != null)
            {
                targetMimic.GetPropertyBlock(_mpb);
                //_mpb.SetFloat("_OuterOutlineFade", _currentOutlineFade);
                _mpb.SetFloat("_StrongTintFade",   _currentTintFade);
                targetMimic.SetPropertyBlock(_mpb);
            }
        }

        public void PlayClip(int clipHash, int layer = -1, float normalizedTime = 0)
        {
            if (layer >= 0 && layer != _currentLayer)
            {
                _spineObjects[_currentLayer].gameObject.SetActive(false);
                if (_skeletonMimics.Length > _currentLayer) _skeletonMimics[_currentLayer].gameObject.SetActive(false);

                _spineObjects[layer].gameObject.SetActive(true);
                if (_skeletonMimics.Length > layer) _skeletonMimics[layer].gameObject.SetActive(true);

                _currentLayer = layer;
                ApplySSUToCurrentMimic();
            }
            _animators[_currentLayer].Play(clipHash, -1, normalizedTime);
        }

        public void SetBool(AnimParamSO param, bool value)
            => _animators[_currentLayer].SetBool(param.ParamHash, value);

        public void SetFloat(AnimParamSO param, float value)
            => _animators[_currentLayer].SetFloat(param.ParamHash, value);

        public void SetInt(AnimParamSO param, int value)
            => _animators[_currentLayer].SetInteger(param.ParamHash, value);

        public void SetTrigger(AnimParamSO param)
            => _animators[_currentLayer].SetTrigger(param.ParamHash);

        public void FlipController(float xMoveDirection)
        {
            if (Mathf.Abs(xMoveDirection) > 0.05f)
            {
                float moveSign = Mathf.Sign(xMoveDirection);
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * -moveSign;
                transform.localScale = scale;
                FacingDirection = moveSign;
            }
        }

        public void Flip()
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
            FacingDirection *= -1;
        }

        internal void EndTrigger() => OnAnimationEnd?.Invoke();
        internal void AttackTrigger() => OnAttackTrigger?.Invoke();
        internal void OpenCounterTrigger() => OnCounterStateChange?.Invoke(true);
        internal void CloseCounterTrigger() => OnCounterStateChange?.Invoke(false);

        public void SetAnimator(RuntimeAnimatorController animator)
        {
            if (_animators != null && _animators.Length > 0 && _animators[0] != null)
            {
                _animators[0].runtimeAnimatorController = animator;
            }
        }
    }
}