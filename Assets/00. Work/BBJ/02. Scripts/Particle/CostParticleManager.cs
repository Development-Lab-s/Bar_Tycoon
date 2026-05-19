using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BBJ.Particle
{
    public class CostParticleManager : MonoBehaviour
    {
        [SerializeField] private CostParticleConfigSO _config;

        private Action<CostParticleEvent> _handler;

        private void Start()
        {
            _config.poolManager.InitializePool(transform);
            _handler = OnParticleEvent;
            _config.particleChannel.AddListener(_handler);
        }

        private void OnDisable()
        {
            _config.particleChannel.RemoveListener(_handler);
        }

        private void OnParticleEvent(CostParticleEvent evt)
        {
            int idx = Unsafe.As<CostParticleType, int>(ref evt.type);
            if ((uint)idx >= (uint)_config.costTypes.Count) return;

            CostTypeConfig config = _config.costTypes[idx];
            CostParticleItem item = _config.poolManager.Pop<CostParticleItem>(_config.particlePoolItem);
            if (item == null) return;

            item.Play(evt.amount, config.spriteIndex, config.gainColor, config.spendColor,
                evt.position, () => _config.poolManager.Push(item));
        }
    }
}
