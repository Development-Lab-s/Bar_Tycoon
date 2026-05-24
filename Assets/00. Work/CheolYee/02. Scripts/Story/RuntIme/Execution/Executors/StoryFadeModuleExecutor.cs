using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Visibility;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces.Executor;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Execution.Executors
{
    public sealed class StoryFadeModuleExecutor : MonoBehaviour, IStoryModuleExecutor
    {
        [SerializeField] private MonoBehaviour fadeControllerSource;

        private IStoryRuntimeFadeController _fadeController;

        private void Awake()
        {
            _fadeController = fadeControllerSource as IStoryRuntimeFadeController;
            if (_fadeController == null && fadeControllerSource == null)
                _fadeController = GetComponentInChildren<StoryRuntimeFadeController>(true);
        }

        public bool CanExecute(StoryModuleSO module) => module is StoryFadeModuleSO;

        public UniTask ExecuteAsync(StoryModuleSO module, StorySession session, CancellationToken ct)
        {
            if (module is not StoryFadeModuleSO fadeModule || _fadeController == null)
                return UniTask.CompletedTask;

            return _fadeController.PlayFadeAsync(fadeModule.Direction, fadeModule.HoldDuration, ct);
        }
    }
}
