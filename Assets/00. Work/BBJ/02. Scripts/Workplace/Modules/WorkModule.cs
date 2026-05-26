using _00._Work._Resources._02._Scripts.Modules;
using Agents.StatSystem;
using BBJ.Order;
using BBJ.UI.Order;
using BBJ.Work;
using BBJ.WorkplaceSystem.Handlers;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

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

        public async UniTask RunAsync(ModuleOwner worker, OrderTicket orderTicket, CancellationToken ct)
        {
            float duration = GetDuration(worker, orderTicket);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                await UniTask.WaitForFixedUpdate(cancellationToken: ct);
                elapsed += Time.fixedDeltaTime;
                OnProgressChanged?.Invoke(elapsed / duration);
            }
        }

        public void NotifyCompleted(ModuleOwner worker, OrderTicket ticket)
        {
            foreach (var handler in _completionHandlers)
                handler.OnCompleted(worker, ticket);
        }

        public void InstantExecute(ModuleOwner executor, OrderTicket ticket)
        {
            _owner.GetModule<WorkplaceQueueModule>() ?.TryCompleteSlotByOwner(ticket.Customer.transform);
            ticket.TryStartProgress(executor);
            NotifyCompleted(executor, ticket);
        }

        public float GetDuration(ModuleOwner worker, OrderTicket orderTicket)
        {
            if (_durationSO == null) return _fallbackDuration;
            return _durationSO.GetDuration(worker.GetModule<IStatModule>(), orderTicket);
        }

        public IEnumerator ExecuteWork(ModuleOwner worker, OrderTicket orderTicket)
        {
            yield return new WaitForSeconds(GetDuration(worker, orderTicket));
        }

        public async UniTask ExecuteWorkAsync(ModuleOwner worker, OrderTicket orderTicket, CancellationToken ct)
        {
            await RunAsync(worker, orderTicket, ct);
            NotifyCompleted(worker, orderTicket);
        }
    }
}
