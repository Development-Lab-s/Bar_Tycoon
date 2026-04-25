using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces.Executor;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Execution.Executors
{
    /// <summary>
    /// StoryCharacterEnterModuleSO 의 런타임 실행을 담당하는 executor.
    /// StoryExecutorRegistry 의 executorSources 또는 자식 오브젝트 자동 스캔으로 등록된다.
    ///
    /// TODO: ICharacterStageDirector 에 ShowCharacterAsync(character, slot, motion, duration, ct) 를
    ///       추가하면 이 executor 에서 호출하도록 확장한다.
    /// </summary>
    public sealed class CharacterEnterModuleExecutor : MonoBehaviour, IStoryModuleExecutor
    {
        public bool CanExecute(StoryModuleSO module) => module is StoryCharacterEnterModuleSO;

        public UniTask ExecuteAsync(StoryModuleSO module, StorySession session, CancellationToken ct)
        {
            var m = (StoryCharacterEnterModuleSO)module;
            // ICharacterStageDirector 확장 전까지는 의도를 로그로 기록
            Debug.Log(
                $"[CharacterEnter] '{m.Character?.DisplayName ?? "?"}' → " +
                $"Slot:{m.Slot}  Motion:{m.Motion}  Duration:{m.Duration:F2}s");
            return UniTask.CompletedTask;
        }
    }
}
