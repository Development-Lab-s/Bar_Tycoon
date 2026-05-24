using System;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Visibility;
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
    public sealed class StoryCoreFacade : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private StoryRunner runner;

        [Header("Channels")]
        [SerializeField] private EventChannelSO storySignalChannel;

        [Header("Optional Services")]
        [SerializeField] private MonoBehaviour logControllerSource;
        [SerializeField] private StorySkipSummaryPanelController skipSummaryPanel;
        [SerializeField] private StoryUiVisibilityController uiVisibilityController;
        [SerializeField] private StoryRuntimeFadeController runtimeFadeController;
        [SerializeField] private StoryEpisodeIntroController episodeIntroController;

        private IStoryLogController _logController;
        private CancellationTokenSource _playCts;

        public bool IsOpen { get; private set; }
        public bool IsLogOpen { get; private set; }
        public bool IsRunning { get; private set; }
        public StorySession Session { get; private set; } = new StorySession();
        public bool IsSkipPopupOpen { get; private set; }

        private void Awake()
        {
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

        public UniTask<StoryPlayResult> PlayAsync(StoryPlayRequest request, CancellationToken ct = default)
        {
            return RunInternalAsync(request, ct);
        }

        public void RequestAdvance()
        {
            if (!IsRunning)
                return;

            runner.RequestAdvance();
        }

        public void RequestSkip()
        {
            if (!IsRunning)
                return;

            runner.RequestSkip();
        }

        public void RequestClose(StoryCloseReason reason)
        {
            if (!IsRunning)
                return;

            runner.Abort(reason);
        }

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

        public void ToggleUiVisible()
        {
            Session.IsUiHidden = !Session.IsUiHidden;

            if (uiVisibilityController == null)
                return;

            if (Session.IsUiHidden)
                uiVisibilityController.Hide();
            else
                uiVisibilityController.Show();
        }

        public void OpenLog()
        {
            IsLogOpen = true;
            _logController?.Open();
        }

        public void CloseLog()
        {
            IsLogOpen = false;
            _logController?.Close();
        }

        private async UniTask<StoryPlayResult> RunInternalAsync(
            StoryPlayRequest request,
            CancellationToken externalCt)
        {
            if (request.Episode == null)
                return new StoryPlayResult(string.Empty, StoryCloseReason.Aborted, false);

            if (IsOpen || IsRunning)
            {
                RequestClose(StoryCloseReason.ExternalRequest);
                await UniTask.WaitUntil(
                    () => !IsOpen && !IsRunning,
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            StoryPlayResult result = default;
            bool hasResult = false;

            PrepareSession(request);

            OpenUi(request.OpenMode);
            IsOpen = true;
            IsRunning = true;

            RaiseSignal(new StoryOpened(Session.Episode.EpisodeId));

            CancellationToken linkedToken = CreateLinkedToken(externalCt);

            try
            {
                await PlayStartupPresentationAsync(request, linkedToken);
                result = await runner.RunAsync(Session, linkedToken);
                hasResult = true;

                if (result.IsCompleted || result.WasSkipped)
                    RaiseSignal(new StoryFinished(result.EpisodeId, result.WasSkipped));

                return result;
            }
            catch (OperationCanceledException)
            {
                result = new StoryPlayResult(
                    Session.Episode != null ? Session.Episode.EpisodeId : string.Empty,
                    StoryCloseReason.Aborted,
                    false);

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

                RaiseSignal(new StoryClosed(episodeId, closeReason));
                CancelPlayLoop();
            }
        }

        private void PrepareSession(StoryPlayRequest request)
        {
            Session.Begin(request.Episode);
            _logController?.Clear();
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

        private void RaiseSignal(GameEvent evt)
        {
            if (storySignalChannel == null)
                return;

            storySignalChannel.RaiseEvent(evt);
        }

        private CancellationToken CreateLinkedToken(CancellationToken externalCt)
        {
            CancelPlayLoop();

            _playCts = CancellationTokenSource.CreateLinkedTokenSource(
                externalCt,
                this.GetCancellationTokenOnDestroy());

            return _playCts.Token;
        }

        private void CancelPlayLoop()
        {
            if (_playCts == null)
                return;

            _playCts.Cancel();
            _playCts.Dispose();
            _playCts = null;
        }

        private async UniTask PlayStartupPresentationAsync(StoryPlayRequest request, CancellationToken ct)
        {
            if (!ShouldPlayStartupPresentation(request))
            {
                uiVisibilityController?.ApplyShownImmediate();
                return;
            }

            uiVisibilityController?.ApplyHiddenImmediate();
            episodeIntroController?.ApplyHiddenImmediate();

            StoryLineSO firstLine = ResolveCurrentLine();
            if (firstLine != null)
                await runner.PreloadLinePresentationAsync(firstLine, ct);

            if (runtimeFadeController != null)
            {
                runtimeFadeController.ApplyBlackImmediate();
                await runtimeFadeController.PlayFadeAsync(Data.Definitions.Modules.StoryFadeDirection.FadeOut, 0f, ct);
            }

            if (episodeIntroController != null)
                await episodeIntroController.PlayAsync(Session.Episode, ct);

            uiVisibilityController?.ApplyShownImmediate();
        }

        private bool ShouldPlayStartupPresentation(StoryPlayRequest request)
        {
            if (request.Episode == null || Session.Episode == null)
                return false;

            return Session.CurrentLineId == request.Episode.EntryLineId;
        }

        private StoryLineSO ResolveCurrentLine()
        {
            if (Session?.Episode == null || string.IsNullOrWhiteSpace(Session.CurrentLineId))
                return null;

            Session.Episode.TryGetLine(Session.CurrentLineId, out StoryLineSO line);
            return line;
        }
    }
}
