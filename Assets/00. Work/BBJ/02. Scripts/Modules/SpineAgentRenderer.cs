using System;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Movement;
using UnityEngine;

namespace BBJ.Modules
{
    public class SpineAgentRenderer : MonoBehaviour, IModule, IRenderer, IAnimatorTrigger
    {
        [SerializeField] private GameObject[] _spineObjects;

        [field: SerializeField] public float FacingDirection { get; private set; } = 1f;

        private IPathMovement _movement;
        private Animator[] _animators;
        private int _currentLayer;

        public event Action OnAnimationEnd;
        public event Action OnAttackTrigger;
        public event Action<bool> OnCounterStateChange;

        public void Initialize(ModuleOwner owner)
        {
            _movement = owner.GetModule<IPathMovement>();

            _animators = new Animator[_spineObjects.Length];
            for (int i = 0; i < _spineObjects.Length; i++)
                _animators[i] = _spineObjects[i].GetComponent<Animator>();

            _currentLayer = 0;
            for (int i = 0; i < _spineObjects.Length; i++)
                _spineObjects[i].SetActive(i == 0);

            var forwarder = _spineObjects[0].GetComponent<AnimationEventForwarder>()
                            ?? _spineObjects[0].AddComponent<AnimationEventForwarder>();
            forwarder.Initialize(this);
        }

        private void Update()
        {
            FlipController(_movement.Velocity.x);
        }

        public void SetAnimator(RuntimeAnimatorController animator)
        {
            _animators[0].runtimeAnimatorController = animator;
        }

        public void PlayClip(int clipHash, int layer = -1, float normalizedTime = 0)
        {
            if (layer >= 0 && layer != _currentLayer)
            {
                _spineObjects[_currentLayer].SetActive(false);
                _spineObjects[layer].SetActive(true);
                _currentLayer = layer;
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
    }
}
