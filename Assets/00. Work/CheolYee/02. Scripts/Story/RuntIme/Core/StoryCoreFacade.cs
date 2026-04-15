using System;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Skip;
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
    /// 스토리 시스템의 핵심 기능을 담당하는 StoryCoreFacade 클래스입니다.
    /// 이 클래스는 스토리 재생과 관련된 주요 로직을 처리하며, 스토리 세션 관리, UI 제어,
    /// 이벤트 신호 발행 등을 담당합니다.
    /// StoryCoreFacade는 MonoBehaviour를 상속하여 Unity 씬에서 활성화된 게임 오브젝트에 부착되어 사용됩니다.
    /// StoryService에서 스토리를 재생할 때 이 클래스를 인스턴스화하여 사용하며,
    /// 스토리 진행 중 다른 시스템과의 상호작용을 관리합니다.
    /// </summary>
    
    public sealed class StoryCoreFacade : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private StoryRunner runner;

        [Header("Channels")]
        [SerializeField] private EventChannelSO storySignalChannel;

        [Header("Optional Services")]
        // 스토리 재개 지점 저장 서비스를 제공하는 컴포넌트입니다. 이 필드는 MonoBehaviour로 선언되어 있지만,
        // 실제로는 IStorySaveService 인터페이스를 구현하는 컴포넌트를 참조해야 합니다.
        [SerializeField] private MonoBehaviour saveServiceSource;
        // 스토리 로그 컨트롤러를 제공하는 컴포넌트입니다. 이 필드는 MonoBehaviour로 선언되어 있지만,
        // 실제로는 IStoryLogController 인터페이스를 구현하는 컴포넌트를 참조해야 합니다.
        [SerializeField] private MonoBehaviour logControllerSource;
        // 스토리 스킵 요약 패널 컨트롤러입니다.
        [SerializeField] private StorySkipSummaryPanelController skipSummaryPanel;


        private IStorySaveService _saveService;
        private IStoryLogController _logController;
        private CancellationTokenSource _playCts;

        public bool IsOpen { get; private set; }
        public bool IsLogOpen { get; private set; }
        public bool IsRunning { get; private set; }
        public StorySession Session { get; private set; } = new StorySession();
        public bool IsSkipPopupOpen { get; private set; }

        private void Awake()
        {
            _saveService = saveServiceSource as IStorySaveService;
            _logController = logControllerSource as IStoryLogController;

            Debug.Assert(runner != null, "StoryRunner reference is missing.");
            SubscribeAll();
        }

        private void OnDestroy()
        {
            CancelPlayLoop();
            UnsubscribeAll();
        }

        #region Event Handlers

        private void SubscribeAll()
        {
            if (skipSummaryPanel != null)
            {
                skipSummaryPanel.CloseRequested += HandleSkipSummaryCloseRequested;
                skipSummaryPanel.ConfirmSkipRequested += HandleSkipSummaryConfirmRequested;
            }
        }

        private void UnsubscribeAll()
        {
            if (skipSummaryPanel != null)
            {
                skipSummaryPanel.CloseRequested -= HandleSkipSummaryCloseRequested;
                skipSummaryPanel.ConfirmSkipRequested -= HandleSkipSummaryConfirmRequested;
            }
        }
        
        private void HandleSkipSummaryCloseRequested()
        {
            CloseSkipSummary();
        }

        private void HandleSkipSummaryConfirmRequested()
        {
            ConfirmSkipSummary();
        }

        #endregion
        
        #region Skip Summary
        
        /// 스토리 스킵 요약 패널을 열기 위한 메서드입니다.
        /// 현재 스토리가 재생 중이고, 세션에 유효한 에피소드가 있으며, 스킵 요약 패널이 할당되어 있는 경우에만 패널을 엽니다.
        /// 이미 스킵 요약 패널이 열려 있는 경우에는 중복으로 열리지 않도록 합니다.
        public void OpenSkipSummary()
        {
            if (!IsOpen || !IsRunning || Session?.Episode == null || skipSummaryPanel == null)
                return;

            if (IsSkipPopupOpen)
                return;

            IsSkipPopupOpen = true;
            skipSummaryPanel.Open(Session.Episode);
        }

        public void CloseSkipSummary()
        {
            if (!IsSkipPopupOpen || skipSummaryPanel == null)
                return;

            IsSkipPopupOpen = false;
            skipSummaryPanel.CloseAnimated();
        }

        public void ConfirmSkipSummary()
        {
            if (!IsSkipPopupOpen)
                return;

            IsSkipPopupOpen = false;

            if (skipSummaryPanel != null)
                skipSummaryPanel.CloseImmediate();

            RequestSkip();
        }
        
        #endregion

        /// StoryRunner의 RunAsync 메서드를 호출하여 스토리를 재생하는 메서드입니다.
        /// 스토리 재생이 완료되면 StoryPlayResult를 반환합니다.
        public UniTask<StoryPlayResult> PlayAsync(StoryPlayRequest request, CancellationToken ct = default)
        {
            return RunInternalAsync(request, hasExplicitResumePoint: false, default, ct);
        }
        
        /// 스토리를 일시 중지한 후, 지정된 재개 지점에서 스토리를 다시 시작하는 메서드입니다.
        public UniTask<StoryPlayResult> ResumeAsync(
            StoryPlayRequest request,
            StoryResumePoint resumePoint,
            CancellationToken ct = default)
        {
            return RunInternalAsync(request, hasExplicitResumePoint: true, resumePoint, ct);
        }
        
        /// 스토리 진행 요청 메서드입니다. 현재 스토리가 재생 중인 경우에만 runner에게 진행 요청을 전달합니다.
        public void RequestAdvance() 
        {
            if (!IsRunning)
                return;

            runner.RequestAdvance();
        }

        /// 스토리 스킵 요청 메서드입니다. 현재 스토리가 재생 중인 경우에만 runner에게 스킵 요청을 전달합니다.
        public void RequestSkip()
        {
            if (!IsRunning)
                return;

            runner.RequestSkip();
        }

        /// 스토리 종료 요청 메서드입니다. 현재 스토리가 재생 중인 경우에만 runner에게 종료 요청을 전달합니다.
        public void RequestClose(StoryCloseReason reason, bool saveResumePoint)
        {
            if (!IsRunning)
                return;

            if (saveResumePoint && Session.Episode != null)
            {
                Session.MarkPendingResume();
                _saveService?.SaveResumePoint(Session);
            }

            runner.Abort(reason);
        }

        /// 스토리 진행 방식을 자동으로 설정하거나 해제하는 메서드입니다.
        /// autoEnabled가 true인 경우 자동 진행 모드로 설정되고,
        /// false인 경우 수동 진행 모드로 설정됩니다.
        public void SetAutoMode(bool autoEnabled)
        {
            Debug.Log($"{autoEnabled} : 자동 모드");
            Session.AdvanceMode = autoEnabled
                ? StoryAdvanceMode.Auto
                : StoryAdvanceMode.Manual;
            
            if (!enabled || runner == null || !runner.IsRunning)
                return;
            
            if (runner.IsWaitingForAdvance)
            {
                runner.RequestAdvance();
            }
        }

        /// 스토리 UI의 표시 여부를 토글하는 메서드입니다.
        /// 현재 UI가 숨겨져 있는지 여부에 따라 UI를 표시하거나 숨깁니다.
        public void ToggleUiVisible()
        {
            Session.IsUiHidden = !Session.IsUiHidden;
        }

        /// 스토리 로그 창을 열거나 닫는 메서드입니다.
        /// 로그 컨트롤러가 존재하는 경우에만 해당 기능이 활성화됩니다.
        public void OpenLog()
        {
            IsLogOpen = true;
            _logController?.Open();
        }
        
        /// 스토리 로그 창을 닫는 메서드입니다.
        public void CloseLog()
        {
            IsLogOpen = false;
            _logController?.Close();
        }
        
        /// 스토리 코어의 전체 시작과 종료를 감싸는 메서드입니다.
        /// UI 오픈, 이벤트 발행, 토큰 생성, 대기 등을 수행하고
        /// 결과에 따라 세이브, 종료, 재진입 등을 발행합니다. 
        private async UniTask<StoryPlayResult> RunInternalAsync( 
            StoryPlayRequest request,
            bool hasExplicitResumePoint,
            StoryResumePoint resumePoint,
            CancellationToken externalCt)
        {
            if (request.Episode == null)
                return new StoryPlayResult(string.Empty, StoryCloseReason.Aborted, false, false);

            if (IsOpen || IsRunning)
            {
                RequestClose(StoryCloseReason.ExternalRequest, saveResumePoint: false);
                await UniTask.WaitUntil(
                    () => !IsOpen && !IsRunning,
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            StoryPlayResult result = default;
            bool hasResult = false;

            PrepareSession(request, hasExplicitResumePoint, resumePoint);

            OpenUi(request.OpenMode);
            IsOpen = true;
            IsRunning = true;

            RaiseSignal(new StoryOpened(Session.Episode.EpisodeId));

            CancellationToken linkedToken = CreateLinkedToken(externalCt);

            try
            {
                result = await runner.RunAsync(Session, linkedToken);
                hasResult = true;

                if (result.IsCompleted || result.WasSkipped)
                {
                    _saveService?.ClearResumePoint(result.EpisodeId);
                    RaiseSignal(new StoryFinished(result.EpisodeId, result.WasSkipped));
                }
                else if (result.HasResumePoint && Session.Episode != null)
                {
                    _saveService?.SaveResumePoint(Session);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                result = new StoryPlayResult(
                    Session.Episode != null ? Session.Episode.EpisodeId : string.Empty,
                    StoryCloseReason.Aborted,
                    false,
                    Session.HasPendingResumePoint);

                hasResult = true;
                return result;
            }
            finally
            {
                CloseUi();
                IsOpen = false;
                IsRunning = false;

                string episodeId = Session.Episode != null ? Session.Episode.EpisodeId : string.Empty;
                StoryCloseReason closeReason = hasResult ? result.CloseReason : StoryCloseReason.Aborted;
                bool hasResumePoint = hasResult ? result.HasResumePoint : Session.HasPendingResumePoint;

                RaiseSignal(new StoryClosed(episodeId, closeReason, hasResumePoint));
                CancelPlayLoop();
            }
        }

        /// 스토리 세션을 준비하는 메서드입니다. 요청된 에피소드로 세션을 초기화하고,
        /// 명시적인 재개 지점이 있는 경우 해당 지점으로 이동하며,
        /// 그렇지 않은 경우 요청에 따라 저장된 재개 지점이 있으면 해당 지점으로 이동합니다.
        /// 또한 로그 컨트롤러가 있는 경우 로그를 초기화합니다.
        private void PrepareSession(
            StoryPlayRequest request,
            bool hasExplicitResumePoint,
            StoryResumePoint resumePoint)
        {
            Session.Begin(request.Episode);
            _logController?.Clear();

            if (hasExplicitResumePoint)
            {
                Session.MoveTo(resumePoint.LineId);
                Session.AdvanceMode = resumePoint.AutoMode
                    ? StoryAdvanceMode.Auto
                    : StoryAdvanceMode.Manual;
                return;
            }

            if (request.UseResumePointIfExists &&
                _saveService != null &&
                _saveService.TryGetResumePoint(request.Episode.EpisodeId, out StoryResumePoint saved))
            {
                Session.MoveTo(saved.LineId);
                Session.AdvanceMode = saved.AutoMode
                    ? StoryAdvanceMode.Auto
                    : StoryAdvanceMode.Manual;
            }
        }

        private void OpenUi(StoryOpenMode openMode)
        {
            // TODO:
            // StoryUIController에 연결해서 Overlay / Fullscreen 처리
            // 현재는 상태만 연다.
        }

        private void CloseUi()
        {
            // TODO:
            // StoryUIController 닫기 처리
        }

        /// 스토리 진행과 관련된 이벤트를 발행하는 메서드입니다.
        /// storySignalChannel이 할당되어 있는 경우에만 이벤트를 발행합니다.
        private void RaiseSignal(GameEvent evt)
        {
            if (storySignalChannel == null)
                return;

            storySignalChannel.RaiseEvent(evt);
        }

        /// 스토리 재생 루프에서 사용할 연결된 취소 토큰을 생성하는 메서드입니다.
        private CancellationToken CreateLinkedToken(CancellationToken externalCt)
        {
            CancelPlayLoop();

            _playCts = CancellationTokenSource.CreateLinkedTokenSource(
                externalCt,
                this.GetCancellationTokenOnDestroy());

            return _playCts.Token;
        }

        /// 현재 스토리 재생 루프에서 사용 중인 취소 토큰을 취소하고 정리하는 메서드입니다.
        private void CancelPlayLoop()
        {
            if (_playCts == null)
                return;

            _playCts.Cancel();
            _playCts.Dispose();
            _playCts = null;
        }
    }
}