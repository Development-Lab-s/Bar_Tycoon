using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Movement;
using UnityEngine;

namespace BBJ.Modules
{
    public class NULLRenderer : MonoBehaviour, IModule, IRenderer
    {
        [field: SerializeField] public float FacingDirection { get; private set; } = 1f;

        private IPathMovement _movement;
        private ModuleOwner _owner;
        private SpriteRenderer _renderer;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _movement = _owner.GetModule<IPathMovement>();
        }

        private void Update()
        {
            FlipController(_movement.Velocity.x);
        }

        public void Flip()
        {
            Vector3 currentScale = transform.localScale;
            currentScale.x *= -1;
            transform.localScale = currentScale;

            FacingDirection *= -1;
        }

        public void FlipController(float xMoveDirection)
        {
            if (Mathf.Abs(xMoveDirection) > 0.05f)
            {
                float moveSign = Mathf.Sign(xMoveDirection);

                Vector3 currentScale = transform.localScale;

                currentScale.x = Mathf.Abs(currentScale.x) * -moveSign;
                transform.localScale = currentScale;
                FacingDirection = moveSign;
            }
        }

        private void ApplyFacingDirection()
        {
            Vector3 currentScale = transform.localScale;

            currentScale.x = Mathf.Abs(currentScale.x) * FacingDirection;

            transform.localScale = currentScale;
        }

        public void PlayClip(int clipHash, int layer = -1, float normalizedTime = 0) { }
        public void SetAnimator(RuntimeAnimatorController animator) { }
        public void SetBool(AnimParamSO param, bool value) { }
        public void SetFloat(AnimParamSO param, float value) { }
        public void SetInt(AnimParamSO param, int value) { }
        public void SetTrigger(AnimParamSO param) { }
    }
}