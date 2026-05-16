using BBJ.Work;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Modules
{
    public class WorkModule : MonoBehaviour, IModule, IWorkExecutor
    {
        [SerializeField] private float _fallbackDuration = 1f;
        [SerializeField] private WorkDurationSO _durationSO;

        public event Action<float> OnProgressChanged;

        public void Initialize(ModuleOwner owner) { }

        public float GetDuration(ModuleOwner worker)
        {
            if (_durationSO == null) return _fallbackDuration;
            return _durationSO.GetDuration(worker);
        }

        public IEnumerator ExecuteWork(ModuleOwner worker)
        {
            yield return new WaitForSeconds(GetDuration(worker));
        }

        public async UniTask ExecuteWorkAsync(ModuleOwner worker, CancellationToken ct)
        {
            float duration = GetDuration(worker);
            float elapsed  = 0f;
            while (elapsed < duration)
            {
                await UniTask.WaitForFixedUpdate(cancellationToken: ct);
                elapsed += Time.fixedDeltaTime;
                OnProgressChanged?.Invoke(elapsed / duration);
            }
        }
    }
}
