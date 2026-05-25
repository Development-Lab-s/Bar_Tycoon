using _00._Work._Resources._02._Scripts.Modules;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;

namespace BBJ.WorkplaceSystem
{
    using BBJ.Order;
    using System;
    public interface IWorkExecutor
    {
        event Action<float> OnProgressChanged;
        float GetDuration(ModuleOwner worker, OrderTicket orderTicket);
        IEnumerator ExecuteWork(ModuleOwner worker, OrderTicket orderTicket);
        UniTask ExecuteWorkAsync(ModuleOwner worker, OrderTicket orderTicket, CancellationToken ct);
    }
}
