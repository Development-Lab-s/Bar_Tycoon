using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    public interface IStoryService
    {
        bool IsStoryOpen { get; }
        string ActiveEpisodeId { get; }
        UniTask<StoryPlayResult> PlayAsync(StoryPlayRequest request, CancellationToken ct = default);
        UniTask CloseActiveStoryAsync(StoryCloseReason reason, CancellationToken ct = default);
    }
}
