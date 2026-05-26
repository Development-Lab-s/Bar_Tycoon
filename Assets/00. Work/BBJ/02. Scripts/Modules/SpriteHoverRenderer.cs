using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;

namespace BBJ.Modules
{
    public class SpriteHoverRenderer : MonoBehaviour, IHoverRenderer
    {
        [SerializeField] private Renderer[] targetRenderers;

        private MaterialPropertyBlock _mpb;
        private float tintValue = 0.21f;

        private static readonly int OuterOutlineFadeID = Shader.PropertyToID("_OuterOutlineFade");
        private static readonly int StrongTintFadeID   = Shader.PropertyToID("_StrongTintFade");

        public void Initialize(ModuleOwner owner)
        {
            _mpb = new MaterialPropertyBlock();

            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);

            DisableHoverEffect();
        }

        // ?? 인터페이스 구현부
        public void EnableHoverEffect() => SetHoverEffect(1f, tintValue);
        public void DisableHoverEffect() => SetHoverEffect(0f, 0f);

        private void SetHoverEffect(float outlineFade, float tintFade)
        {
            if (targetRenderers == null || _mpb == null) return;

            foreach (var renderer in targetRenderers)
            {
                if (renderer == null) continue;

                renderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(OuterOutlineFadeID, outlineFade);
                _mpb.SetFloat(StrongTintFadeID, tintFade);
                renderer.SetPropertyBlock(_mpb);
            }
        }
    }
}
