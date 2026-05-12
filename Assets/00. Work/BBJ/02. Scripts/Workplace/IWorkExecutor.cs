using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem
{
    public interface IWorkExecutor
    {
        event Action<float> OnProgressChanged;
        float GetDuration(ModuleOwner worker);
        IEnumerator ExecuteWork(ModuleOwner worker);
        UniTask ExecuteWorkAsync(ModuleOwner worker, CancellationToken ct);
    }
}
