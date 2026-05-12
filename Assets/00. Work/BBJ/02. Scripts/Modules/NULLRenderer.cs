using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using UnityEngine;
namespace BBJ.Modules
{

    public class NULLRenderer : MonoBehaviour, IModule, IRenderer
    {
        public float FacingDirection { get; }

        public void Flip()
        {
        }

        public void FlipController(float xMoveDirection)
        {
        }

        public void Initialize(ModuleOwner owner)
        {
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