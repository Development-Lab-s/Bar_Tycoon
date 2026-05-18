using _00._Work._Resources._02._Scripts.Modules;
using BBJ.Actions;
using Cysharp.Threading.Tasks;
using System;

namespace BBJ.Modules
{
    public interface IAgentActionModule
    {
        UniTask Execute<T>(Func<T, UniTask> execute) where T : class, IAgentAction;
        T GetAction<T>() where T : class, IAgentAction;
    }
}