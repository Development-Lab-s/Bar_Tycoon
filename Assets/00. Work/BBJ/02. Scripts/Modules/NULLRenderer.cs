using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using BBJ.Movement;
using UnityEngine;
namespace BBJ.Modules
{

    public class NULLRenderer : MonoBehaviour, IModule, IRenderer, IAfterInitModule
    {
        [field: SerializeField] public float FacingDirection { get; private set; } = 1f;

        private IPathMovement _movement;
        private ModuleOwner _owner;
        private SpriteRenderer _renderer;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _renderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void AfterInit()
        {
            _movement = _owner.GetModule<IPathMovement>();
        }
        private void Update()
        {
            FlipController(_movement.Velocity.x);
        }
        public void Flip()
        {
            FacingDirection *= -1;
            float targetYRotation = FacingDirection > 0 ? 0 : 180f;
            _owner.transform.rotation = Quaternion.Euler(0, targetYRotation, 0);
        }

        public void FlipController(float xMoveDirection)
        {

            if (Mathf.Abs(FacingDirection + xMoveDirection) < 0.5f)
                Flip();
        }


        public void PlayClip(int clipHash, int layer = -1, float normalizedTime = 0)
        {
        }

        public void SetAnimator(RuntimeAnimatorController animator)
        {
        }

        public void SetBool(AnimParamSO param, bool value)
        {
        }

        public void SetFloat(AnimParamSO param, float value)
        {
        }

        public void SetInt(AnimParamSO param, int value)
        {
        }

        public void SetTrigger(AnimParamSO param)
        {
        }
    }

}