using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Collections;
using System.Threading;

namespace BBJ.Actions
{
    public interface IAgentAction
    {
        IEnumerator Execute(GameEvent gameEvent);
        UniTask ExecuteAsync(GameEvent gameEvent, CancellationToken ct);
    }
}