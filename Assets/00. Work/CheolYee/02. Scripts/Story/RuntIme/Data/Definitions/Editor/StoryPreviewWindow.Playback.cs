using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed partial class StoryPreviewWindow
    {
        // ?? 踰꾪듉 ?몃뱾????????????????????????????????

        private void OnPlay()
        {
            if (episode == null) return;
            if (!episode.TryGetLine(episode.EntryLineId, out var entry)) return;

            _isPlaying    = true;
            _isLineSample = false;
            ShowLineSnapshot(entry);
            RefreshButtons();
        }

        private void OnFromHere()
        {
            if (_pendingFromLine == null) return;

            _isPlaying    = true;
            _isLineSample = false;
            ShowLineSnapshot(_pendingFromLine);
            RefreshButtons();
        }

        private void OnSampleLine()
        {
            if (_pendingFromLine == null) return;

            _isLineSample = true;
            ShowLineSnapshot(_pendingFromLine);
            RefreshButtons();
        }

        private void OnStop() => StopPlayback();

        private void OnNext()
        {
            if (_currentLine == null || string.IsNullOrEmpty(_currentLine.NextLineId)) return;
            if (episode == null || !episode.TryGetLine(_currentLine.NextLineId, out var next)) return;

            ShowLineSnapshot(next);
            RefreshButtons();
        }

        // ?? ?ъ깮 ?곹깭 珥덇린???????????????????????????

        private void StopPlayback()
        {
            _isPlaying    = false;
            _isLineSample = false;
            _currentLine  = null;
            _stageState.Clear();
            _bgState = null;
            RebuildActorLayer();
            RefreshDialogue();
            RefreshChoices();
            RefreshButtons();
        }

        // ?? ?ㅻ깄???쒖떆 ??????????????????????????????

        /// <summary>?ъ깮 ?놁씠 ?뱀젙 ?쇱씤???ㅽ뀒?댁? ?곹깭瑜??ㅻ깄?룹쑝濡??쒖떆?쒕떎.</summary>
        private void ShowLineSnapshot(StoryLineSO line)
        {
            _currentLine = line;
            BuildStageStateAt(line);
            ValidateStageSelection();
            RebuildActorLayer();
            RefreshActorInspector();
            RefreshDialogue();
            RefreshChoices();
        }

        // ?? ?ㅽ뀒?댁? ?곹깭 ?꾩쟻 ????????????????????????

        /// <summary>
        /// episode 吏꾩엯 ?쇱씤遺??targetLine 源뚯? nextLineId 泥댁씤???곕씪
        /// StoryStageLayoutModuleSO 瑜??곸슜??_stageState / _bgState 瑜?援ъ텞?쒕떎.
        /// targetLine ??泥댁씤???놁쑝硫?怨좎븘 ?쇱씤) targetLine ?먯껜 紐⑤뱢留??곸슜?쒕떎.
        /// </summary>
        private void BuildStageStateAt(StoryLineSO targetLine)
        {
            _stageState.Clear();
            _bgState = null;

            if (episode == null || targetLine == null) return;

            string currentId = episode.EntryLineId;
            const int maxSteps = 500;
            int steps = 0;

            bool found = false;
            while (!string.IsNullOrEmpty(currentId) && steps < maxSteps)
            {
                if (!episode.TryGetLine(currentId, out var line)) break;

                ApplyStageModulesToState(line);

                if (line == targetLine) { found = true; break; }

                currentId = line.NextLineId;
                steps++;
            }

            if (!found)
            {
                _stageState.Clear();
                _bgState = null;
                ApplyStageModulesToState(targetLine);
            }
        }

        private void ApplyStageModulesToState(StoryLineSO line)
        {
            if (line == null) return;

            foreach (var module in line.Modules)
            {
                if (module is not StoryStageLayoutModuleSO layout) continue;

                if (layout.HasBackground)
                    _bgState = layout.Background.ShallowClone();

                foreach (var actorData in layout.Actors)
                {
                    if (actorData == null) continue;

                    StoryActorStateData clone = actorData.ShallowClone();
                    string actorKey = clone.ResolvedActorKey;
                    if (string.IsNullOrWhiteSpace(actorKey)) continue;

                    _stageState[actorKey] = clone;
                }
            }
        }

        // ?? ???/ ?좏깮吏 媛깆떊 ???????????????????????

        private void RefreshDialogue()
        {
            string speaker = _currentLine?.GetResolvedSpeakerName() ?? "";
            string text    = _currentLine?.DialogueText ?? "";

            if (_speakerLabel  != null) _speakerLabel.text  = speaker;
            if (_dialogueLabel != null) _dialogueLabel.text = text;

            if (_renderSpeakerLabel  != null) _renderSpeakerLabel.text  = speaker;
            if (_renderDialogueLabel != null) _renderDialogueLabel.text = text;

            bool hasContent = !string.IsNullOrEmpty(text);
            SetElementVisible(_renderDialoguePanel,
                hasContent && previewMode == PreviewMode.RuntimePreview && !_previewRuntimeUiCollapsed);
        }

        private void RefreshChoices()
        {
            _choiceArea?.Clear();
            _renderChoiceArea?.Clear();

            IStoryChoiceLikeModule choiceLike = FindChoiceLikeModule(_currentLine);
            if (choiceLike != null)
                BuildChoiceButtons(choiceLike);

            ApplyPreviewModeVisibility();
        }

        private static IStoryChoiceLikeModule FindChoiceLikeModule(StoryLineSO line)
        {
            if (line == null)
                return null;

            foreach (var module in line.Modules)
            {
                if (module is IStoryChoiceLikeModule choiceLike)
                    return choiceLike;
            }

            return null;
        }

        private void BuildChoiceButtons(IStoryChoiceLikeModule choiceLike)
        {
            if (choiceLike.Options == null || choiceLike.Options.Count == 0)
                return;

            _choiceArea?.Add(new Label("Choices")
            {
                style =
                {
                    fontSize = 10,
                    color = new StyleColor(new Color(1f, 0.78f, 0.35f)),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginRight = 6,
                    marginTop = 3
                }
            });

            foreach (IStoryChoiceOption option in choiceLike.Options)
            {
                string reactionId = option.ReactionStartLineId;
                string text = string.IsNullOrWhiteSpace(option.Text) ? "(Empty Choice)" : option.Text;

                var bottomButton = new Button(() => OnChoiceSelected(reactionId))
                {
                    text = text,
                    style =
                    {
                        marginRight = 6,
                        marginBottom = 4,
                        paddingLeft = 8,
                        paddingRight = 8,
                        whiteSpace = WhiteSpace.Normal,
                        maxWidth = 220
                    }
                };
                _choiceArea?.Add(bottomButton);

                var overlayButton = new Button(() => OnChoiceSelected(reactionId))
                {
                    text = text,
                    style =
                    {
                        marginBottom = 5,
                        paddingLeft = 10,
                        paddingRight = 10,
                        minHeight = 28,
                        whiteSpace = WhiteSpace.Normal,
                        unityTextAlign = TextAnchor.MiddleLeft,
                        backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.1f, 0.9f)),
                        color = new StyleColor(new Color(0.94f, 0.94f, 0.96f))
                    }
                };
                _renderChoiceArea?.Add(overlayButton);
            }
        }

        private void OnChoiceSelected(string reactionStartLineId)
        {
            if (string.IsNullOrWhiteSpace(reactionStartLineId))
            {
                StopPlayback();
                return;
            }

            if (episode == null || !episode.TryGetLine(reactionStartLineId, out var next))
                return;

            if (!_isPlaying)
                _isLineSample = true;

            ShowLineSnapshot(next);
            RefreshButtons();
        }

        // ?? 踰꾪듉 媛?쒖꽦 / ?곹깭 ???????????????????????

        private void RefreshButtons()
        {
            bool isRuntime  = previewMode == PreviewMode.RuntimePreview;
            bool hasEpisode = episode != null;

            SetElementVisible(_playBtn,      isRuntime && !_isPlaying && hasEpisode);
            SetElementVisible(_fromHereBtn,  isRuntime && !_isPlaying && hasEpisode && _pendingFromLine != null);
            SetElementVisible(_stopBtn,      isRuntime && _isPlaying);
            SetElementVisible(_nextBtn,      isRuntime && _isPlaying);
            SetElementVisible(_statusLabel,  isRuntime && !_previewRuntimeUiCollapsed);
            SetElementVisible(_sampleLineBtn, hasEpisode);

            if (_nextBtn != null)
                _nextBtn.SetEnabled(_currentLine != null && !string.IsNullOrEmpty(_currentLine.NextLineId));

            if (_statusLabel != null)
            {
                _statusLabel.text = _isPlaying
                    ? (_isLineSample ? "Sample" : "Playing")
                    : "Stopped";
            }
        }

        // ?? 紐⑤뱶 ?꾪솚 媛?쒖꽦 ?????????????????????????

        /// <summary>RuntimePreview / StageAuthoring 紐⑤뱶???곕씪 UI ?붿냼 媛?쒖꽦??議곗젙?쒕떎.</summary>
        private void ApplyPreviewModeVisibility()
        {
            bool isRuntime = previewMode == PreviewMode.RuntimePreview;
            bool showRuntimeUi = isRuntime && !_previewRuntimeUiCollapsed;

            SetElementVisible(_dialoguePanel, showRuntimeUi);
            SetElementVisible(_choiceArea,    showRuntimeUi);
            SetElementVisible(_authoringToolsRoot, !isRuntime);
            SetElementVisible(_authoringGridLayer, !isRuntime);
            SetElementVisible(_cameraGizmoLayer, !isRuntime);

            SetElementVisible(_renderDialoguePanel,
                showRuntimeUi && _renderDialoguePanel != null
                          && !string.IsNullOrEmpty(_renderDialogueLabel?.text));
            SetElementVisible(_renderChoiceArea, showRuntimeUi);

            RefreshButtons();
            RefreshAuthoringControls();
        }
    }
}
