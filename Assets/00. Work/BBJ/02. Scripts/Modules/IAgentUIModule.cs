using System.Threading;
using BBJ.UI;
using Cysharp.Threading.Tasks;

namespace BBJ.Modules
{
    public interface IAgentUIModule
    {
        T Get<T>() where T : class, IAgentUI;
        UniTask PlaySequenceAsync(CancellationToken ct, params IAgentUI[] sequence);
        void CancelSequence();
        void CloseAll();
    }
}
