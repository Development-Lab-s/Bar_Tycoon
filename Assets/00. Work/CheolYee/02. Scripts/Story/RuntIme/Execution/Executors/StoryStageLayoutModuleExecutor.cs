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
    /// <summary>
    /// StoryStageLayoutModuleSO 를 실행하는 런타임 executor.
    /// IStoryStageDirector 를 통해 enter / move / exit / focus 를 처리한다.
    ///
    /// ■ 씬 배치:
    ///   StoryExecutorRegistry 의 executorSources 에 추가하거나
    ///   자동 스캔(빈 리스트)으로 자식 오브젝트에서 발견되게 한다.
    ///   stageDirectorSource 에 BasicStoryCharacterStageDirector 를 연결한다.
    /// </summary>
    public sealed class StoryStageLayoutModuleExecutor : MonoBehaviour, IStoryModuleExecutor
    {
        [Tooltip("IStoryStageDirector 구현체 (BasicStoryCharacterStageDirector 등)")]
        [SerializeField] private MonoBehaviour stageDirectorSource;

        private IStoryStageDirector _stageDirector;

        private void Awake()
        {
            _stageDirector = stageDirectorSource as IStoryStageDirector;
            if (_stageDirector == null)
                Debug.LogWarning($"[{nameof(StoryStageLayoutModuleExecutor)}] " +
                                 "stageDirectorSource 가 IStoryStageDirector 를 구현하지 않습니다.", this);
        }

        public bool CanExecute(StoryModuleSO module) => module is StoryStageLayoutModuleSO;

        public UniTask ExecuteAsync(StoryModuleSO module, StorySession session, CancellationToken ct)
        {
            var layout = (StoryStageLayoutModuleSO)module;

            if (_stageDirector == null)
            {
                // fallback: 의도 로깅
                if (layout.HasBackground)
                {
                    var background = layout.Background;
                    Debug.Log($"[StageLayout] background={background.ResolvedBackgroundKey} " +
                              $"visible={background.visible} opacity={background.EffectiveOpacity}");
                }

                foreach (var actor in layout.Actors)
                {
                    if (actor == null)
                        continue;

                    Debug.Log($"[StageLayout] {actor.actor?.DisplayName ?? actor.ResolvedActorKey} " +
                              $"pos={actor.normalizedPosition}  visible={actor.visible}  focused={actor.focused}");
                }
                return UniTask.CompletedTask;
            }

            return _stageDirector.ApplyStageStateAsync(layout.Actors, ct);
        }
    }
}
