using _00._Work._Resources._02._Scripts.Modules;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;

namespace BBJ.WorkplaceSystem
{
    using System;
    public interface IWorkExecutor
    {
        event Action<float> OnProgressChanged;
        float GetDuration(ModuleOwner worker);
        IEnumerator ExecuteWork(ModuleOwner worker);
        UniTask ExecuteWorkAsync(ModuleOwner worker, CancellationToken ct);
    }
}
