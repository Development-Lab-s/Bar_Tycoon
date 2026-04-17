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
    public sealed class CharacterClearModuleExecutor : MonoBehaviour, IStoryModuleExecutor, IStoryInlineModuleExecutor
    {
        [SerializeField] private MonoBehaviour characterStageSource;

        private ICharacterStageDirector _characterStage;

        private void Awake()
        {
            _characterStage = characterStageSource as ICharacterStageDirector;
            Debug.Assert(_characterStage != null, "ICharacterStageDirector implementation is missing.");
        }

        // --- SO 모듈 처리 ---

        public bool CanExecute(StoryModuleSO module) => module is StoryCharacterClearModuleSO;

        public UniTask ExecuteAsync(StoryModuleSO module, StorySession session, CancellationToken ct)
        {
            if (module is not StoryCharacterClearModuleSO)
                return UniTask.CompletedTask;

            _characterStage?.ClearAll();
            return UniTask.CompletedTask;
        }

        // --- 인라인 모듈 처리 ---

        public bool CanExecute(StoryInlineModuleData module) => module is CharacterClearInlineModuleData;

        public UniTask ExecuteAsync(StoryInlineModuleData module, StorySession session, CancellationToken ct)
        {
            if (module is not CharacterClearInlineModuleData)
                return UniTask.CompletedTask;

            _characterStage?.ClearAll();
            return UniTask.CompletedTask;
        }
    }
}
