using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.RuntimeModules;
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
    /// 스토리 세션을 받아 에피소드를 순차 재생하는 핵심 Runner.
    /// Choice 처리는 IStoryChoiceLikeModule capability 로 통일되어,
    /// SO 경로와 인라인 경로를 동일하게 다룹니다.
    /// </summary>
    public sealed class StoryRunner : MonoBehaviour
    {
        [Header("Directors")]
        [SerializeField] private MonoBehaviour textDirectorSource;
        [SerializeField] private MonoBehaviour choicePanelSource;
        [SerializeField] private MonoBehaviour characterStageSource;
        [SerializeField] private MonoBehaviour logControllerSource;
        [SerializeField] private MonoBehaviour executorRegistrySource;

        [Header("Channels")]
        [SerializeField] private EventChannelSO storySignalChannel;

        [Header("Timing")]
        [SerializeField] private float defaultAutoAdvanceDelay = 0.8f;

        private ITextDirector          _textDirector;
        private IChoicePanelController _choicePanel;
        private ICharacterStageDirector _characterStage;
        private IStoryLogController    _logController;
        private IStoryExecutorRegistry _executorRegistry;

        private bool _advanceRequested;
        private bool _skipRequested;
        private bool _abortRequested;
        private StoryCloseReason _abortReason = StoryCloseReason.Aborted;

        public bool IsRunning          { get; private set; }
        public bool IsWaitingForAdvance { get; private set; }
        public bool IsTyping   => _textDirector is { IsTyping: true };
        public bool IsChoiceOpen => _choicePanel is { IsChoiceOpen: true };

        private void Awake()
        {
            _textDirector     = textDirectorSource     as ITextDirector;
            _choicePanel      = choicePanelSource      as IChoicePanelController;
            _characterStage   = characterStageSource   as ICharacterStageDirector;
            _logController    = logControllerSource    as IStoryLogController;
            _executorRegistry = executorRegistrySource as IStoryExecutorRegistry;

            Debug.Assert(_textDirector   != null, "TextDirector implementation is missing.");
            Debug.Assert(_choicePanel    != null, "ChoicePanelController implementation is missing.");
            Debug.Assert(_characterStage != null, "CharacterStageDirector implementation is missing.");
            Debug.Assert(_logController  != null, "StoryLogController implementation is missing.");
        }

        public async UniTask<StoryPlayResult> RunAsync(StorySession session, CancellationToken ct = default)
        {
            if (session == null || session.Episode == null)
                return new StoryPlayResult(string.Empty, StoryCloseReason.Aborted, false, false);

            ResetFlags();
            IsRunning = true;

            try
            {
                RaiseSignal(new StoryStarted(session.Episode.EpisodeId, session.CurrentLineId));

                while (!ct.IsCancellationRequested)
                {
                    if (_abortRequested)
                        return CreateResult(session, _abortReason, false);

                    if (_skipRequested)
                        return CreateResult(session, StoryCloseReason.Skipped, true);

                    if (!session.Episode.TryGetLine(session.CurrentLineId, out StoryLineSO line) || line == null)
                        return CreateResult(session, StoryCloseReason.Aborted, false);

                    await ExecuteModulesAsync(line, StoryModuleTiming.BeforeDialogue, session, ct);

                    await _characterStage.EnsureSpeakerVisibleAsync(line, ct);
                    _characterStage.ApplySpeakerFocus(line);

                    UniTask withModulesTask = ExecuteModulesAsync(
                        line,
                        StoryModuleTiming.WithDialogue,
                        session,
                        ct);

                    await _textDirector.PlayLineAsync(line, ct);
                    await withModulesTask;

                    if (_abortRequested)
                        return CreateResult(session, _abortReason, false);

                    if (_skipRequested)
                        return CreateResult(session, StoryCloseReason.Skipped, true);

                    await ExecuteModulesAsync(line, StoryModuleTiming.AfterDialogue, session, ct);

                    if (line.LogVisible)
                        AppendLineLog(session, line);

                    // ── Choice 처리: SO / 인라인 모두 IStoryChoiceLikeModule 로 통일 ──
                    bool hasChoice = false;
                    StoryChoiceResult choiceResult = default;

                    if (TryGetChoiceLikeModule(line, out IStoryChoiceLikeModule choiceLike))
                    {
                        hasChoice    = true;
                        choiceResult = await _choicePanel.ShowChoicesAsync(choiceLike, ct);
                    }

                    if (hasChoice)
                    {
                        session.ChoiceResults[choiceResult.ChoiceId] = choiceResult.OptionId;
                        AppendChoiceLog(session, line, choiceResult);
                        RaiseSignal(new StoryChoiceCommitted(
                            session.Episode.EpisodeId,
                            choiceResult.ChoiceId,
                            choiceResult.OptionId));

                        if (!string.IsNullOrWhiteSpace(choiceResult.NextLineId))
                        {
                            session.MoveTo(choiceResult.NextLineId);
                            continue;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(line.NextLineId))
                        return CreateResult(session, StoryCloseReason.Completed, false);

                    if (ShouldAutoAdvance(session, line))
                    {
                        float delay = line.UseAutoAdvanceOverride
                            ? line.AutoAdvanceDelay
                            : defaultAutoAdvanceDelay;

                        await WaitAutoAdvanceDelayAsync(delay, ct);
                    }
                    else
                    {
                        await WaitForAdvanceAsync(ct);
                    }

                    if (_abortRequested)
                        return CreateResult(session, _abortReason, false);

                    if (_skipRequested)
                        return CreateResult(session, StoryCloseReason.Skipped, true);

                    session.MoveTo(line.NextLineId);
                }

                return CreateResult(session, StoryCloseReason.Aborted, false);
            }
            finally
            {
                IsRunning           = false;
                IsWaitingForAdvance = false;
                _textDirector?.Clear();
                _choicePanel?.CloseImmediate();
            }
        }

        public void RequestAdvance()
        {
            if (!IsRunning) return;
            _advanceRequested = true;
        }

        public void RequestSkip()
        {
            if (!IsRunning) return;
            _skipRequested = true;
            _textDirector?.CompleteCurrentLine();
            _choicePanel?.CompleteReveal();
        }

        public void Abort(StoryCloseReason reason)
        {
            if (!IsRunning) return;
            _abortRequested = true;
            _abortReason    = reason;
            _textDirector?.CompleteCurrentLine();
            _choicePanel?.CloseImmediate();
        }

        // ── 내부 헬퍼 ────────────────────────────────────────────────────────

        private bool TryGetChoiceLikeModule(StoryLineSO line, out IStoryChoiceLikeModule choiceModule)
        {
            foreach (StoryModuleSO module in line.Modules)
            {
                if (module is IStoryChoiceLikeModule choice)
                {
                    choiceModule = choice;
                    return true;
                }
            }

            choiceModule = null;
            return false;
        }

        private async UniTask ExecuteModulesAsync(
            StoryLineSO line,
            StoryModuleTiming timing,
            StorySession session,
            CancellationToken ct)
        {
            if (_executorRegistry == null) return;
            await _executorRegistry.ExecuteModulesAsync(line, timing, session, ct);
        }

        private async UniTask WaitForAdvanceAsync(CancellationToken ct)
        {
            IsWaitingForAdvance = true;
            _advanceRequested   = false;

            try
            {
                while (!_advanceRequested && !_skipRequested && !_abortRequested)
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            finally
            {
                IsWaitingForAdvance = false;
                _advanceRequested   = false;
            }
        }

        private async UniTask WaitAutoAdvanceDelayAsync(float delaySeconds, CancellationToken ct)
        {
            if (delaySeconds <= 0f) return;

            float elapsed = 0f;
            _advanceRequested = false;

            while (elapsed < delaySeconds && !_advanceRequested && !_skipRequested && !_abortRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.unscaledDeltaTime;
            }

            _advanceRequested = false;
        }

        private bool ShouldAutoAdvance(StorySession session, StoryLineSO line)
        {
            if (line.UseAutoAdvanceOverride) return true;
            return session.AdvanceMode == StoryAdvanceMode.Auto;
        }

        private void AppendLineLog(StorySession session, StoryLineSO line)
        {
            StoryLogEntry entry = new StoryLogEntry
            {
                entryType   = line.IsNarration() ? StoryLogEntryType.Narration : StoryLogEntryType.Dialogue,
                episodeId   = session.Episode.EpisodeId,
                lineId      = line.LineId,
                speaker     = line.Speaker,
                displayName = line.GetResolvedSpeakerName(),
                text        = line.DialogueText,
                voice       = line.Voice,
                sequence    = session.Logs.Count,
            };

            session.Logs.Add(entry);
            _logController.AppendLine(entry);
        }

        private void AppendChoiceLog(
            StorySession session,
            StoryLineSO line,
            StoryChoiceResult choiceResult)
        {
            StoryLogEntry entry = new StoryLogEntry
            {
                entryType   = StoryLogEntryType.ChoiceResult,
                episodeId   = session.Episode.EpisodeId,
                lineId      = line.LineId,
                speaker     = null,
                displayName = string.Empty,
                text        = choiceResult.DisplayText,
                voice       = null,
                sequence    = session.Logs.Count,
            };

            session.Logs.Add(entry);
            _logController.AppendChoiceResult(entry);
        }

        private StoryPlayResult CreateResult(
            StorySession session,
            StoryCloseReason closeReason,
            bool wasSkipped)
        {
            bool hasResumePoint = closeReason == StoryCloseReason.UserClosed
                                  || closeReason == StoryCloseReason.ExternalRequest
                                  || session.HasPendingResumePoint;

            return new StoryPlayResult(
                session.Episode != null ? session.Episode.EpisodeId : string.Empty,
                closeReason,
                wasSkipped,
                hasResumePoint);
        }

        private void ResetFlags()
        {
            _advanceRequested = false;
            _skipRequested    = false;
            _abortRequested   = false;
            _abortReason      = StoryCloseReason.Aborted;
        }

        private void RaiseSignal(GameEvent evt)
        {
            if (storySignalChannel == null) return;
            storySignalChannel.RaiseEvent(evt);
        }
    }
}
