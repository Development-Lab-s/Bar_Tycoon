using BBJ.Order;
using BBJ.Work;
using BBJ.WorkplaceSystem.Handlers;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Modules
{
    public class WorkModule : MonoBehaviour, IModule, IWorkExecutor
    {
        [SerializeField] private float _fallbackDuration = 1f;
        [SerializeField] private WorkDurationSO _durationSO;
        [SerializeField] private List<WorkCompletionHandlerSO> _completionHandlers = new();

        public event Action<float> OnProgressChanged;

        private ModuleOwner _owner;
        public void Initialize(ModuleOwner owner) { _owner = owner; }

        public float GetDuration(ModuleOwner worker)
        {
            if (_durationSO == null) return _fallbackDuration;
            return _durationSO.GetDuration(worker);
        }

        public IEnumerator ExecuteWork(ModuleOwner worker)
        {
            yield return new WaitForSeconds(GetDuration(worker));
        }

        public async UniTask RunAsync(ModuleOwner worker, CancellationToken ct)
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

        public async UniTask ExecuteWorkAsync(ModuleOwner worker, CancellationToken ct)
        {
            await RunAsync(worker, ct);
            NotifyCompleted(worker, worker.transform.position);
        }

        public void NotifyCompleted(ModuleOwner worker, Vector3 position)
        {
            foreach (var handler in _completionHandlers)
                handler.OnCompleted(worker, position);
        }

        public void InstantExecute(ModuleOwner executor, OrderTicket ticket)
        {
            _owner.GetModule<WorkplaceQueueModule>()
                  ?.TryCompleteSlotByOwner(ticket.Customer.transform);
            ticket.TryStartProgress(executor);
            NotifyCompleted(executor, executor.transform.position);
        }
    }
}
