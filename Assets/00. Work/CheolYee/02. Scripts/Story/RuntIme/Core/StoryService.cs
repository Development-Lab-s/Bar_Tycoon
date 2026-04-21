using System.Collections.Generic;
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
    /// <summary>
    /// 스토리 시스템의 핵심 서비스인 StoryService 클래스입니다.
    /// 이 클래스는 스토리 재생, 재개, 종료 요청을 처리하고, 스토리 에피소드 카탈로그를 관리하며,
    /// 선택적으로 스토리 재개 지점 저장 서비스를 통합합니다.
    /// StoryService는 MonoBehaviour를 상속하여 Unity 씬에서 활성화된 게임 오브젝트에 부착되어 사용됩니다.
    /// 스토리 시스템과 상호작용하는 다른 시스템들은 StoryService의 공개 메서드를 통해
    /// 스토리를 제어할 수 있으며, 이벤트 채널을 통해 스토리 관련 명령을 수신하여 처리할 수 있습니다.
    /// </summary>
    public sealed class StoryService : MonoBehaviour, IStoryService
    {
        [Header("Channels")]
        [SerializeField] private EventChannelSO storyCommandChannel;

        [Header("Core")]
        [SerializeField] private StoryCoreFacade storyCorePrefab; //스토리를 재생할 때 이 프리팹을 인스턴스화하여 사용합니다.
        [SerializeField] private Transform storyRoot;

        [Header("Episode Catalog")]
        //스토리 시스템에서 사용할 수 있는 에피소드들의 카탈로그입니다.
        [SerializeField] private List<StoryEpisodeSO> episodes = new();

        [Header("Optional Services")]
        //스토리 재개 지점 저장 서비스를 제공하는 컴포넌트입니다. 이 필드는 MonoBehaviour로 선언되어 있지만,
        //실제로는 IStorySaveService 인터페이스를 구현하는 컴포넌트를 참조해야 합니다.
        [SerializeField] private MonoBehaviour saveServiceSource;

        private IStorySaveService _saveService;
        private StoryCoreFacade _activeCore;

        public bool IsStoryOpen => _activeCore != null && _activeCore.IsOpen;
        public string ActiveEpisodeId =>
            _activeCore != null && _activeCore.Session?.Episode != null
                ? _activeCore.Session.Episode.EpisodeId
                : string.Empty;

        private void Awake()
        {
            _saveService = saveServiceSource as IStorySaveService;

            if (storyCommandChannel == null)
                return;

            storyCommandChannel.AddListener<PlayStoryRequested>(HandlePlayStoryRequested);
            storyCommandChannel.AddListener<ResumeStoryRequested>(HandleResumeStoryRequested);
            storyCommandChannel.AddListener<CloseStoryRequested>(HandleCloseStoryRequested);
            storyCommandChannel.AddListener<SkipStoryRequested>(HandleSkipStoryRequested);
        }

        private void OnDestroy()
        {
            if (storyCommandChannel == null)
                return;

            storyCommandChannel.RemoveListener<PlayStoryRequested>(HandlePlayStoryRequested);
            storyCommandChannel.RemoveListener<ResumeStoryRequested>(HandleResumeStoryRequested);
            storyCommandChannel.RemoveListener<CloseStoryRequested>(HandleCloseStoryRequested);
            storyCommandChannel.RemoveListener<SkipStoryRequested>(HandleSkipStoryRequested);
        }

        #region IStoryService 인터페이스 구현부

        /// IStoryService 인터페이스 구현
        /// PlayAsync 메서드는 스토리 재생 요청을 처리합니다. 요청된 에피소드가 유효한지 확인하고,
        /// 현재 활성화된 스토리가 있다면 이를 종료한 후 새로운 스토리를 재생합니다.
        public async UniTask<StoryPlayResult> PlayAsync(StoryPlayRequest request, CancellationToken ct = default)
        {
            if (request.Episode == null)
                return new StoryPlayResult(string.Empty, StoryCloseReason.Aborted, false, false);

            StoryCoreFacade core = EnsureCore();

            if (core.IsOpen || core.IsRunning)
            {
                await CloseActiveStoryAsync(
                    StoryCloseReason.ExternalRequest,
                    saveResumePoint: false,
                    ct);
            }

            return await core.PlayAsync(request, ct);
        }

        /// ResumeAsync 메서드는 스토리 재개 요청을 처리합니다. 요청된 에피소드 ID가 유효한지 확인하고,
        /// 현재 활성화된 스토리가 있다면 이를 종료한 후 해당 에피소드의 재개 지점이 존재하면 재개 지점부터,
        /// 그렇지 않으면 처음부터 스토리를 재생합니다.
        public async UniTask<StoryPlayResult> ResumeAsync(string episodeId, CancellationToken ct = default)
        {
            if (!TryFindEpisode(episodeId, out StoryEpisodeSO episode))
                return new StoryPlayResult(episodeId, StoryCloseReason.Aborted, false, false);

            StoryPlayRequest request = new StoryPlayRequest(
                episode);

            StoryCoreFacade core = EnsureCore();

            if (core.IsOpen || core.IsRunning)
            {
                await CloseActiveStoryAsync(
                    StoryCloseReason.ExternalRequest,
                    saveResumePoint: false,
                    ct);
            }

            if (_saveService != null && _saveService.TryGetResumePoint(episodeId, out StoryResumePoint resumePoint))
            {
                return await core.ResumeAsync(request, resumePoint, ct);
            }

            return await core.PlayAsync(request, ct);
        }

        /// CloseActiveStoryAsync 메서드는 현재 활성화된 스토리를 종료하는 요청을 처리합니다.
        /// 스토리가 열려 있거나 실행 중인 경우,
        /// 지정된 종료 이유와 재개 지점 저장 여부에 따라 스토리를 종료하고, 종료가 완료될 때까지 대기합니다.
        public async UniTask CloseActiveStoryAsync(
            StoryCloseReason reason,
            bool saveResumePoint,
            CancellationToken ct = default)
        {
            if (_activeCore == null || (!_activeCore.IsOpen && !_activeCore.IsRunning))
                return;

            _activeCore.RequestClose(reason, saveResumePoint);

            await UniTask.WaitUntil(
                () => !_activeCore.IsOpen && !_activeCore.IsRunning,
                cancellationToken: ct);
        }

        /// TryGetResumePoint 메서드는 지정된 에피소드 ID에 대한 재개 지점이 존재하는지 확인하고,
        /// 존재한다면 해당 재개 지점을 반환합니다.
        public bool TryGetResumePoint(string episodeId, out StoryResumePoint resumePoint)
        {
            if (_saveService != null)
                return _saveService.TryGetResumePoint(episodeId, out resumePoint);

            resumePoint = default;
            return false;
        }

        #endregion
        
        /// EnsureCore 메서드는 스토리 재생에 사용할 StoryCoreFacade 인스턴스를 반환합니다.
        /// 이미 활성화된 코어가 있다면 이를 반환하고,
        /// 그렇지 않다면 storyCorePrefab을 인스턴스화하여 새로운 코어를 생성하고 반환합니다.
        /// 이 메서드는 스토리 재생 요청을 처리하기 전에 항상 호출되어야 합니다.
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

        /// TryFindEpisode 메서드는 지정된 에피소드 ID에 해당하는 StoryEpisodeSO를 카탈로그에서 검색합니다.
        /// 검색에 성공하면 true를 반환하고, 검색된 에피소드를 out 매개변수로 반환합니다.
        private bool TryFindEpisode(string episodeId, out StoryEpisodeSO episode)
        {
            for (int i = 0; i < episodes.Count; i++)
            {
                StoryEpisodeSO candidate = episodes[i];
                if (candidate != null && candidate.EpisodeId == episodeId)
                {
                    episode = candidate;
                    return true;
                }
            }

            episode = null;
            return false;
        }

        #region Handles
        /// 이벤트 핸들러 메서드들입니다.
        /// 각 메서드는 해당 이벤트가 발생했을 때 스토리 시스템이 어떻게 반응해야 하는지를 정의합니다.
        private void HandlePlayStoryRequested(PlayStoryRequested evt)
        {
            PlayAsync(
                new StoryPlayRequest(
                    evt.Episode,
                    evt.OpenMode,
                    evt.UseResumePointIfExists,
                    evt.CallerId),
                this.GetCancellationTokenOnDestroy()).Forget();
        }


        /// HandleResumeStoryRequested 메서드는 ResumeStoryRequested 이벤트가 발생했을 때 호출됩니다.
        private void HandleResumeStoryRequested(ResumeStoryRequested evt)
        {
            ResumeAsync(evt.EpisodeId, this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// HandleCloseStoryRequested 메서드는 CloseStoryRequested 이벤트가 발생했을 때 호출됩니다.
        private void HandleCloseStoryRequested(CloseStoryRequested evt)
        {
            CloseActiveStoryAsync(
                evt.Reason,
                evt.SaveResumePoint,
                this.GetCancellationTokenOnDestroy()).Forget();
        }

        // HandleSkipStoryRequested 메서드는 SkipStoryRequested 이벤트가 발생했을 때 호출됩니다.
        private void HandleSkipStoryRequested(SkipStoryRequested evt)
        {
            if (_activeCore == null || !_activeCore.IsRunning)
                return;

            _activeCore.RequestSkip();
        }
        #endregion
    }
}