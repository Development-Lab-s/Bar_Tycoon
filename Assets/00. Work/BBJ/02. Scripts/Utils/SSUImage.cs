using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ
{
    public class SSUImage : Image, IStylableUI
    {
        private Material _instancedMaterial;

        private Material InstancedMaterial
        {
            get
            {
                if (_instancedMaterial == null && this.material != null)
                {
                    _instancedMaterial = Instantiate(this.material);
                    this.material = _instancedMaterial;
                }
                return _instancedMaterial;
            }
        }

        // 동적 프로퍼티 ID 캐싱 시스템
        private static readonly Dictionary<string, int> PropertyIdCache = new Dictionary<string, int>();

        private static int GetPropertyID(string propertyName)
        {
            if (!PropertyIdCache.TryGetValue(propertyName, out int id))
            {
                id = Shader.PropertyToID(propertyName);
                PropertyIdCache[propertyName] = id;
            }
            return id;
        }

        protected override void Start()
        {
            base.Start();
            _ = InstancedMaterial;
        }

        // ==========================================
        // 빌더 패턴 (메서드 체이닝) 적용
        // ==========================================

        public IStylableUI SetRecolorFade(float fadeValue)
        {
            if (InstancedMaterial != null)
                InstancedMaterial.SetFloat(GetPropertyID("_RecolorRGBFade"), fadeValue);
            return this; // 자기 자신을 반환
        }

        public IStylableUI SetFlashAmount(float amount)
        {
            if (InstancedMaterial != null)
                InstancedMaterial.SetFloat(GetPropertyID("_FlashAmount"), amount);
            return this;
        }

        public IStylableUI SetFlashColor(Color color)
        {
            if (InstancedMaterial != null)
                InstancedMaterial.SetColor(GetPropertyID("_FlashColor"), color);
            return this;
        }

        public IStylableUI SetOutlineFade(float fadeValue)
        {
            if (InstancedMaterial != null)
                InstancedMaterial.SetFloat(GetPropertyID("_OutlineFade"), fadeValue);
            return this;
        }

        public IStylableUI SetCustomShaderFloat(string propertyName, float value)
        {
            if (InstancedMaterial != null)
                InstancedMaterial.SetFloat(GetPropertyID(propertyName), value);
            return this;
        }

        // (보너스) Image의 기본 기능도 체이닝할 수 있게 감싸주면 아주 좋습니다.
        public IStylableUI SetColor(Color color)
        {
            this.color = color;
            return this;
        }

        public IStylableUI SetSprite(Sprite sprite)
        {
            this.sprite = sprite;
            return this;
        }
    }
}