using System.Collections.Generic;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    public sealed class BasicStoryChoicePanelController : MonoBehaviour, IChoicePanelController
    {
        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform optionRoot;
        [SerializeField] private Button optionButtonPrefab;

        [Header("Reveal")]
        [SerializeField] private float revealDuration = 0.20f;

        private readonly List<Button> _spawnedButtons = new();
        private UniTaskCompletionSource<StoryChoiceResult> _selectionTcs;
        private bool _completeRevealRequested;
        private bool _resultResolved;

        public bool IsRevealing { get; private set; }
        public bool IsChoiceOpen { get; private set; }

        public async UniTask<StoryChoiceResult> ShowChoicesAsync(StoryChoiceModuleSO module, CancellationToken ct)
        {
            if (module == null || module.Options == null || module.Options.Count == 0)
            {
                CloseImmediate();
                return default;
            }

            Build(module);

            IsRevealing = true;
            IsChoiceOpen = false;
            _completeRevealRequested = false;

            try
            {
                await WaitRevealAsync(ct);

                IsRevealing = false;
                IsChoiceOpen = true;
                SetButtonsInteractable(true);

                _selectionTcs = new UniTaskCompletionSource<StoryChoiceResult>();
                _resultResolved = false;

                using (ct.Register(() => _selectionTcs.TrySetCanceled()))
                {
                    StoryChoiceResult result = await _selectionTcs.Task;
                    return result;
                }
            }
            finally
            {
                IsRevealing = false;
                IsChoiceOpen = false;
            }
        }

        public void CompleteReveal()
        {
            if (!IsRevealing)
                return;

            _completeRevealRequested = true;
        }

        public void CloseImmediate()
        {
            IsRevealing = false;
            IsChoiceOpen = false;
            _completeRevealRequested = false;
            _resultResolved = false;

            if (_selectionTcs != null)
            {
                _selectionTcs.TrySetCanceled();
                _selectionTcs = null;
            }

            ClearButtons();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void Build(StoryChoiceModuleSO module)
        {
            ClearButtons();

            if (panelRoot != null)
                panelRoot.SetActive(true);

            foreach (var option in module.Options)
            {
                Button button = Instantiate(optionButtonPrefab, optionRoot);
                button.interactable = false;

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = option.text;

                button.onClick.RemoveAllListeners();
                var option1 = option;
                button.onClick.AddListener(() => SelectOption(module, option1));

                _spawnedButtons.Add(button);
            }
        }

        private async UniTask WaitRevealAsync(CancellationToken ct)
        {
            if (revealDuration <= 0f)
                return;

            float elapsed = 0f;
            while (!_completeRevealRequested && elapsed < revealDuration)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.unscaledDeltaTime;
            }
        }

        private void SelectOption(StoryChoiceModuleSO module, StoryChoiceModuleSO.ChoiceOption option)
        {
            if (!IsChoiceOpen || _resultResolved || _selectionTcs == null)
                return;

            _resultResolved = true;

            string nextLineId = !string.IsNullOrWhiteSpace(option.reactionStartLineId)
                ? option.reactionStartLineId
                : option.convergeLineId;

            _selectionTcs.TrySetResult(new StoryChoiceResult(
                module.ChoiceId,
                option.optionId,
                option.text,
                nextLineId));

            CloseImmediate();
        }

        private void SetButtonsInteractable(bool value)
        {
            foreach (var button in _spawnedButtons)
            {
                if (button != null)
                    button.interactable = value;
            }
        }

        private void ClearButtons()
        {
            foreach (var button in _spawnedButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }

            _spawnedButtons.Clear();
        }
    }
}