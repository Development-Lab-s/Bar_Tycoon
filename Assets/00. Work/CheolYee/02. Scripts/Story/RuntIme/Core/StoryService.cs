using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core
{
    public sealed class StoryService : MonoBehaviour, IStoryService
    {
        [Header("Channels")]
        [SerializeField] private EventChannelSO storyCommandChannel;

        [Header("Core")]
        [SerializeField] private StoryCoreFacade storyCorePrefab;
        [SerializeField] private Transform storyRoot;

        private StoryCoreFacade _activeCore;

        public bool IsStoryOpen => _activeCore != null && _activeCore.IsOpen;
        public string ActiveEpisodeId =>
            _activeCore != null && _activeCore.Session?.Episode != null
                ? _activeCore.Session.Episode.EpisodeId
                : string.Empty;

        private void Awake()
        {
            if (storyCommandChannel == null)
                return;

            storyCommandChannel.AddListener<PlayStoryRequested>(HandlePlayStoryRequested);
            storyCommandChannel.AddListener<CloseStoryRequested>(HandleCloseStoryRequested);
            storyCommandChannel.AddListener<SkipStoryRequested>(HandleSkipStoryRequested);
        }

        private void OnDestroy()
        {
            if (storyCommandChannel == null)
                return;

            storyCommandChannel.RemoveListener<PlayStoryRequested>(HandlePlayStoryRequested);
            storyCommandChannel.RemoveListener<CloseStoryRequested>(HandleCloseStoryRequested);
            storyCommandChannel.RemoveListener<SkipStoryRequested>(HandleSkipStoryRequested);
        }

        public async UniTask<StoryPlayResult> PlayAsync(StoryPlayRequest request, CancellationToken ct = default)
        {
            if (request.Episode == null)
                return new StoryPlayResult(string.Empty, StoryCloseReason.Aborted, false);

            StoryCoreFacade core = EnsureCore();

            if (core.IsOpen || core.IsRunning)
                await CloseActiveStoryAsync(StoryCloseReason.ExternalRequest, ct);

            return await core.PlayAsync(request, ct);
        }

        public async UniTask CloseActiveStoryAsync(StoryCloseReason reason, CancellationToken ct = default)
        {
            if (_activeCore == null || (!_activeCore.IsOpen && !_activeCore.IsRunning))
                return;

            _activeCore.RequestClose(reason);

            await UniTask.WaitUntil(
                () => !_activeCore.IsOpen && !_activeCore.IsRunning,
                cancellationToken: ct);
        }

        private StoryCoreFacade EnsureCore()
        {
            if (_activeCore != null)
                return _activeCore;

            Debug.Assert(storyCorePrefab != null, "StoryCoreFacade 프리팹이 할당되지 않았습니다. " +
                                                  "StoryService는 스토리를 재생하기 위해 " +
                                                  "StoryCoreFacade 프리팹이 필요합니다.");

            Transform parent = storyRoot != null ? storyRoot : transform;
            _activeCore = Instantiate(storyCorePrefab, parent);
            return _activeCore;
        }

        private void HandlePlayStoryRequested(PlayStoryRequested evt)
        {
            PlayAsync(
                new StoryPlayRequest(evt.Episode, evt.OpenMode, evt.CallerId),
                this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void HandleCloseStoryRequested(CloseStoryRequested evt)
        {
            CloseActiveStoryAsync(evt.Reason, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void HandleSkipStoryRequested(SkipStoryRequested evt)
        {
            if (_activeCore == null || !_activeCore.IsRunning)
                return;

            _activeCore.RequestSkip();
        }
    }
}
