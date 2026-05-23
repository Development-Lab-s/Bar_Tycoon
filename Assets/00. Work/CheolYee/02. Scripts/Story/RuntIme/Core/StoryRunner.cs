using System;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.RuntimeModules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
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
        private EventChannelSO _activeStoryBgmChannel;
        private float _activeStoryBgmFadeDuration;

        private static readonly System.Collections.Generic.HashSet<string> MissingSoundChannelWarnings = new();

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
            bool isFirstPresentedLine = true;

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

                    StoryStageLayoutModuleSO stageLayout = FindStageLayout(line);
                    CancellationTokenSource lineSoundCts = null;
                    UniTask lineSoundTask = UniTask.CompletedTask;

                    try
                    {
                        if (ShouldPreapplyStageLayoutBeforeDialogue(line, stageLayout))
                            _characterStage?.ApplyStageLayoutImmediate(stageLayout);

                        await ExecuteModulesAsync(line, StoryModuleTiming.BeforeDialogue, session, ct);

                        await _characterStage.EnsureSpeakerVisibleAsync(line, ct);
                        _characterStage.ApplySpeakerFocus(line);

                        UniTask withModulesTask = ExecuteModulesAsync(
                            line,
                            StoryModuleTiming.WithDialogue,
                            session,
                            ct);

                        if (isFirstPresentedLine && stageLayout != null)
                        {
                            // Let the first stage layout instantiate and render once before
                            // audio/text start so startup hitch does not cause audio-first playback.
                            await WarmupFirstPresentationFrameAsync(ct);
                        }

                        lineSoundCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        lineSoundTask = RunLineSoundTrackAsync(session.Episode, line, stageLayout, lineSoundCts.Token);

                        await _textDirector.PlayLineAsync(line, ct);
                        isFirstPresentedLine = false;
                        await withModulesTask;

                        if (_abortRequested)
                            return CreateResult(session, _abortReason, false);

                        if (_skipRequested)
                            return CreateResult(session, StoryCloseReason.Skipped, true);

                        if (line.LogVisible)
                            AppendLineLog(session, line);

                        bool hasChoice = false;
                        StoryChoiceResult choiceResult = default;

                        if (TryGetChoiceLikeModule(line, out IStoryChoiceLikeModule choiceLike))
                        {
                            hasChoice = true;
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
                                await ExecuteModulesAsync(line, StoryModuleTiming.AfterDialogue, session, ct);
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

                        await ExecuteModulesAsync(line, StoryModuleTiming.AfterDialogue, session, ct);
                        session.MoveTo(line.NextLineId);
                    }
                    finally
                    {
                        await CleanupLineSoundAsync(session.Episode, stageLayout, lineSoundCts, lineSoundTask);
                    }
                }

                return CreateResult(session, StoryCloseReason.Aborted, false);
            }
            finally
            {
                IsRunning           = false;
                IsWaitingForAdvance = false;
                StopActiveStoryBgm();
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

        public void PreloadLineBackground(StoryLineSO line)
        {
            if (line == null || _characterStage == null)
                return;

            StoryStageLayoutModuleSO layout = FindStageLayout(line);
            _characterStage.ApplyBackgroundOnly(layout);
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

        private static bool ShouldPreapplyStageLayoutBeforeDialogue(
            StoryLineSO line,
            StoryStageLayoutModuleSO stageLayout)
        {
            if (line == null || stageLayout == null || line.Modules == null)
                return false;

            foreach (StoryModuleSO module in line.Modules)
            {
                if (module is StoryFadeModuleSO fade
                    && fade.Timing == StoryModuleTiming.BeforeDialogue
                    && fade.Direction == StoryFadeDirection.FadeOut)
                    return true;
            }

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

        private static StoryStageLayoutModuleSO FindStageLayout(StoryLineSO line)
        {
            if (line == null)
                return null;

            foreach (StoryModuleSO module in line.Modules)
            {
                if (module is StoryStageLayoutModuleSO layout)
                    return layout;
            }

            return null;
        }

        private async UniTask RunLineSoundTrackAsync(
            StoryEpisodeSO episode,
            StoryLineSO line,
            StoryStageLayoutModuleSO layout,
            CancellationToken ct)
        {
            if (line == null || layout == null)
                return;

            StorySoundSettingsData settings = StorySoundSettingsUtility.ResolveEffectiveLineSettings(episode, layout);
            StorySoundTrackData track = layout.SoundTrack;
            if (settings == null || track == null)
                return;

            EventChannelSO channel = settings.soundChannel;
            if (channel == null)
            {
                WarnMissingSoundChannel(line, layout);
                return;
            }

            UniTask bgmTask = RunBgmTrackAsync(track, settings, ct);
            UniTask sfxTask = RunSfxTrackAsync(track, settings, ct);
            await UniTask.WhenAll(bgmTask, sfxTask);
        }

        private async UniTask RunBgmTrackAsync(StorySoundTrackData track, StorySoundSettingsData settings, CancellationToken ct)
        {
            if (track?.bgmKeyframes == null || track.bgmKeyframes.Count == 0)
                return;

            var ordered = new System.Collections.Generic.List<StoryBgmKeyframeData>(track.bgmKeyframes);
            ordered.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));

            float elapsed = 0f;
            foreach (StoryBgmKeyframeData keyframe in ordered)
            {
                if (keyframe == null)
                    continue;

                elapsed = await WaitForSoundKeyTimeAsync(keyframe.timeSeconds, elapsed, ct);
                ct.ThrowIfCancellationRequested();

                if (keyframe.operation == StoryBgmKeyOperation.Stop)
                {
                    StopStoryBgm(settings.soundChannel, settings.bgmFadeDuration);
                    continue;
                }

                bool crossfade = keyframe.transitionMode == StoryBgmTransitionMode.Crossfade;
                PlayStoryBgm(settings, keyframe.bgmSound, crossfade);
            }
        }

        private async UniTask RunSfxTrackAsync(StorySoundTrackData track, StorySoundSettingsData settings, CancellationToken ct)
        {
            if (track?.sfxKeyframes == null || track.sfxKeyframes.Count == 0)
                return;

            var ordered = new System.Collections.Generic.List<StorySfxKeyframeData>(track.sfxKeyframes);
            ordered.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));

            float elapsed = 0f;
            foreach (StorySfxKeyframeData keyframe in ordered)
            {
                if (keyframe == null)
                    continue;

                elapsed = await WaitForSoundKeyTimeAsync(keyframe.timeSeconds, elapsed, ct);
                ct.ThrowIfCancellationRequested();

                Vector3 position = _characterStage != null ? _characterStage.GetCurrentCameraCenter() : Vector3.zero;
                settings.soundChannel.RaiseEvent(new PlayManagedSoundEvent(
                    keyframe.sfxSound,
                    position,
                    SoundChannelId.StorySfx,
                    settings.sfxFadeDuration,
                    settings.sfxFadeDuration));
            }
        }

        private static async UniTask<float> WaitForSoundKeyTimeAsync(float targetTime, float elapsed, CancellationToken ct)
        {
            targetTime = Mathf.Max(0f, targetTime);
            while (elapsed < targetTime)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.unscaledDeltaTime;
            }
            return elapsed;
        }

        private static async UniTask WarmupFirstPresentationFrameAsync(CancellationToken ct)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, ct);
        }

        private async UniTask CleanupLineSoundAsync(
            StoryEpisodeSO episode,
            StoryStageLayoutModuleSO layout,
            CancellationTokenSource lineSoundCts,
            UniTask lineSoundTask)
        {
            if (lineSoundCts != null && !lineSoundCts.IsCancellationRequested)
                lineSoundCts.Cancel();

            try
            {
                await lineSoundTask;
            }
            catch (OperationCanceledException)
            {
                // line lifetime ended before future sound keys fired
            }
            finally
            {
                lineSoundCts?.Dispose();
            }

            StorySoundSettingsData settings = StorySoundSettingsUtility.ResolveEffectiveLineSettings(episode, layout);
            EventChannelSO soundChannel = settings?.soundChannel;
            if (soundChannel != null)
            {
                float fadeDuration = settings.sfxFadeDuration;
                soundChannel.RaiseEvent(new StopManagedSoundEvent(SoundChannelId.StorySfx, fadeDuration));
            }
        }

        private void PlayStoryBgm(StorySoundSettingsData settings, BgmSounds bgmSound, bool crossfade)
        {
            if (settings?.soundChannel == null)
                return;

            if (_activeStoryBgmChannel != null && _activeStoryBgmChannel != settings.soundChannel)
            {
                float previousFade = crossfade ? _activeStoryBgmFadeDuration : 0f;
                _activeStoryBgmChannel.RaiseEvent(new StopManagedSoundEvent(SoundChannelId.Bgm, previousFade));
            }

            Vector3 position = _characterStage != null ? _characterStage.GetCurrentCameraCenter() : Vector3.zero;
            settings.soundChannel.RaiseEvent(new PlayManagedSoundEvent(
                bgmSound,
                position,
                SoundChannelId.Bgm,
                settings.bgmFadeDuration,
                settings.bgmFadeDuration,
                crossfade));

            _activeStoryBgmChannel = settings.soundChannel;
            _activeStoryBgmFadeDuration = settings.bgmFadeDuration;
        }

        private void StopStoryBgm(EventChannelSO soundChannel, float fadeDuration)
        {
            EventChannelSO targetChannel = _activeStoryBgmChannel ?? soundChannel;
            if (targetChannel == null)
                return;

            targetChannel.RaiseEvent(new StopManagedSoundEvent(SoundChannelId.Bgm, fadeDuration));
            if (targetChannel == _activeStoryBgmChannel)
            {
                _activeStoryBgmChannel = null;
                _activeStoryBgmFadeDuration = 0f;
            }
        }

        private void StopActiveStoryBgm()
        {
            if (_activeStoryBgmChannel == null)
                return;

            _activeStoryBgmChannel.RaiseEvent(new StopManagedSoundEvent(SoundChannelId.Bgm, _activeStoryBgmFadeDuration));
            _activeStoryBgmChannel = null;
            _activeStoryBgmFadeDuration = 0f;
        }

        private static void WarnMissingSoundChannel(StoryLineSO line, StoryStageLayoutModuleSO layout)
        {
            if (line == null || layout == null)
                return;

            string warningKey = $"{layout.GetInstanceID()}::{line.LineId}";
            if (!MissingSoundChannelWarnings.Add(warningKey))
                return;

            Debug.LogWarning(
                $"[{nameof(StoryRunner)}] Line '{line.LineId}' has sound keyframes but no EventChannelSO assigned in StorySoundSettingsData. Sound keys will be skipped.",
                layout);
        }
    }
}
