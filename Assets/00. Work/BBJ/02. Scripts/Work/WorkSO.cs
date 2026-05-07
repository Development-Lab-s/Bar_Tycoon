using BBJ.Staff;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Threading;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public abstract class WorkSO : ScriptableObject
    {
        public AgentRole RequiredRole;
        public abstract UniTask ExecuteAsync(ModuleOwner executor, GameEvent context, CancellationToken ct);
    }
}
