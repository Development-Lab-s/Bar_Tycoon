using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using Cysharp.Threading.Tasks;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    public interface IStoryRuntimeFadeController
    {
        float CurrentAlpha { get; }
        void ApplyBlackImmediate();
        void ApplyClearImmediate();
        UniTask PlayFadeAsync(StoryFadeDirection direction, CancellationToken ct);
    }
}
