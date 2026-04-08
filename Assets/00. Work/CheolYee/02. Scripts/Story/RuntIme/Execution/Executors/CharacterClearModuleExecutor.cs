using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces.Executor;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Execution.Executors
{
    public sealed class CharacterClearModuleExecutor : MonoBehaviour, IStoryModuleExecutor
    {
        [SerializeField] private MonoBehaviour characterStageSource;

        private ICharacterStageDirector _characterStage;

        private void Awake()
        {
            _characterStage = characterStageSource as ICharacterStageDirector;
            Debug.Assert(_characterStage != null, "ICharacterStageDirector implementation is missing.");
        }

        public bool CanExecute(StoryModuleSO module)
        {
            return module is StoryCharacterClearModuleSO;
        }

        public UniTask ExecuteAsync(StoryModuleSO module, StorySession session, CancellationToken ct)
        {
            if (module is not StoryCharacterClearModuleSO)
                return UniTask.CompletedTask;

            _characterStage?.ClearAll();
            return UniTask.CompletedTask;
        }
    }
}