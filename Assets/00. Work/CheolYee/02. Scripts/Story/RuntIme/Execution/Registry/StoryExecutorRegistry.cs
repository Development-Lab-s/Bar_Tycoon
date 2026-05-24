using System.Collections.Generic;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces.Executor;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Execution.Registry
{
    /// <summary>
    /// SO 모듈 executor 를 관리하는 레지스트리.
    ///
    /// ■ Executor 등록 방식 — 두 가지 옵션:
    ///
    ///   (A) 명시적 등록 (권장):
    ///       executorSources 리스트에 Inspector에서 원하는 MonoBehaviour를 직접 지정합니다.
    ///
    ///   (B) 자동 스캔 (fallback):
    ///       executorSources 가 비어 있을 때 동일 GameObject와 자식 오브젝트에서
    ///       IStoryModuleExecutor 를 자동으로 수집합니다.
    /// </summary>
    public sealed class StoryExecutorRegistry : MonoBehaviour, IStoryExecutorRegistry
    {
        [Tooltip("실행할 executor MonoBehaviour 목록. 비워 두면 자식 오브젝트에서 자동 스캔합니다.")]
        [SerializeField] private List<MonoBehaviour> executorSources = new();

        private readonly List<IStoryModuleExecutor> _executors = new();

        private void Awake()
        {
            _executors.Clear();

            List<MonoBehaviour> effectiveSources = BuildEffectiveSources();

            foreach (MonoBehaviour src in effectiveSources)
            {
                if (src == null) continue;

                if (src is IStoryModuleExecutor executor)
                {
                    _executors.Add(executor);
                }
                else
                {
                    Debug.LogError(
                        $"'{src.GetType().Name}' 은 IStoryModuleExecutor 를 구현하지 않습니다.",
                        src);
                }
            }
        }

        public async UniTask ExecuteModulesAsync(
            StoryLineSO line,
            StoryModuleTiming timing,
            StorySession session,
            CancellationToken ct)
        {
            if (line.Modules == null || line.Modules.Count == 0)
                return;

            // WithDialogue modules run in parallel so FadeOut and stage/sound/text all start together.
            // BeforeDialogue and AfterDialogue remain sequential.
            if (timing == StoryModuleTiming.WithDialogue)
            {
                var tasks = new System.Collections.Generic.List<UniTask>();
                foreach (StoryModuleSO module in line.Modules)
                {
                    ct.ThrowIfCancellationRequested();
                    if (module == null || module.Timing != timing) continue;
                    if (module is IStoryChoiceLikeModule) continue;

                    IStoryModuleExecutor executor = FindExecutor(module);
                    if (executor == null)
                    {
                        Debug.LogWarning(
                            $"모듈 '{module.name}' ({module.GetType().Name}) 에 대한 executor 를 찾을 수 없습니다.",
                            module);
                        continue;
                    }
                    tasks.Add(executor.ExecuteAsync(module, session, ct));
                }
                if (tasks.Count > 0)
                    await UniTask.WhenAll(tasks);
            }
            else
            {
                foreach (StoryModuleSO storyModuleSO in line.Modules)
                {
                    ct.ThrowIfCancellationRequested();

                    StoryModuleSO module = storyModuleSO;
                    if (module == null || module.Timing != timing)
                        continue;

                    // Choice 계열은 StoryRunner 가 IStoryChoiceLikeModule 로 처리
                    if (module is IStoryChoiceLikeModule)
                        continue;

                    IStoryModuleExecutor executor = FindExecutor(module);
                    if (executor == null)
                    {
                        Debug.LogWarning(
                            $"모듈 '{module.name}' ({module.GetType().Name}) 에 대한 executor 를 찾을 수 없습니다.",
                            module);
                        continue;
                    }

                    await executor.ExecuteAsync(module, session, ct);
                }
            }
        }

        // ── 내부 헬퍼 ───────────────────────────────────────────────────────

        private List<MonoBehaviour> BuildEffectiveSources()
        {
            if (executorSources.Count > 0)
                return executorSources;

            var scanned = new List<MonoBehaviour>();
            foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is IStoryModuleExecutor)
                    scanned.Add(mb);
            }

            if (scanned.Count == 0)
                Debug.LogWarning($"[{nameof(StoryExecutorRegistry)}] executor 가 하나도 등록되지 않았습니다. " +
                                 "executorSources 를 Inspector 에서 설정하거나 자식 오브젝트에 executor 를 배치하세요.", this);

            return scanned;
        }

        private IStoryModuleExecutor FindExecutor(StoryModuleSO module)
        {
            foreach (IStoryModuleExecutor executor in _executors)
            {
                if (executor != null && executor.CanExecute(module))
                    return executor;
            }

            return null;
        }
    }
}
