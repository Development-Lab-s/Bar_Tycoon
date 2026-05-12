using BBJ.Staff.FSM;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using System.Collections;
using System.Threading;

namespace BBJ.Actions
{
    public interface IActionDispatcher
    {
        IEnumerator ExecuteState(TycoonAgentAction newStateIndex, GameEvent gameEvent);
        UniTask ExecuteStateAsync(TycoonAgentAction newStateIndex, GameEvent gameEvent, CancellationToken ct);
    }
}
