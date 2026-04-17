using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using Cysharp.Threading.Tasks;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces.Executor
{
    public interface IStoryInlineModuleExecutor
    {
        bool CanExecute(StoryInlineModuleData module);
        UniTask ExecuteAsync(StoryInlineModuleData module, StorySession session, CancellationToken ct);
    }
}
