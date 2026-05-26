using System;
using System.Collections;
using Gamelib.ObjectPool.Runtime;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Pool
{
    [RequireComponent(typeof(ParticleSystem))]
    public class SungtaeParticle : PoolableMono
    {
        [SerializeField] private ParticleSystem particle;
        [SerializeField] private ParticleSystemRenderer particleRenderer;

        private Coroutine _returnCoroutine;
        private Action _onComplete;

        private void Awake()
        {
            if (particle == null)
                particle = GetComponent<ParticleSystem>();
            
            if (particleRenderer == null)
                particleRenderer = GetComponent<ParticleSystemRenderer>();
        }
        
        public void Play(Vector3 position, Material material, Action onComplete)
        {
            _onComplete = onComplete;
            transform.position = position;

            if (material != null && particleRenderer != null)
            {
                particleRenderer.sharedMaterial = material;
            }

            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Clear();
            particle.Play();

            _returnCoroutine = StartCoroutine(ReturnWhenFinished());
        }
        
        private IEnumerator ReturnWhenFinished()
        {
            // 파티클이 살아있는 동안 기다림
            while (particle.IsAlive(true))
            {
                yield return null;
            }

            _onComplete?.Invoke();
        }

        public override void ResetItem()
        {
            base.ResetItem();
            
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
            
            _onComplete = null;

            if (particle != null)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Clear();
            }
        }
    }
}