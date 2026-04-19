using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using Cysharp.Threading.Tasks;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces.Executor
{
    public interface IStoryModuleExecutor
    {
        bool CanExecute(StoryModuleSO module);
        UniTask ExecuteAsync(StoryModuleSO module, StorySession session, CancellationToken ct);
    }
}