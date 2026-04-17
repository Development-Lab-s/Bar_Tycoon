using System.Collections.Generic;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces.Executor;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Execution.Registry
{
    public sealed class StoryExecutorRegistry : MonoBehaviour, IStoryExecutorRegistry
    {
        [SerializeField] private List<MonoBehaviour> executorSources = new();

        private readonly List<IStoryModuleExecutor> _executors = new();
        private readonly List<IStoryInlineModuleExecutor> _inlineExecutors = new();

        private void Awake()
        {
            _executors.Clear();
            _inlineExecutors.Clear();

            for (int i = 0; i < executorSources.Count; i++)
            {
                MonoBehaviour src = executorSources[i];
                if (src == null) continue;

                bool registered = false;

                if (src is IStoryModuleExecutor executor)
                {
                    _executors.Add(executor);
                    registered = true;
                }

                if (src is IStoryInlineModuleExecutor inlineExecutor)
                {
                    _inlineExecutors.Add(inlineExecutor);
                    registered = true;
                }

                if (!registered)
                {
                    Debug.LogError(
                        $"'{src.GetType().Name}' does not implement IStoryModuleExecutor or IStoryInlineModuleExecutor.",
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
            await ExecuteSoModulesAsync(line, timing, session, ct);
            await ExecuteInlineModulesAsync(line, timing, session, ct);
        }

        private async UniTask ExecuteSoModulesAsync(
            StoryLineSO line,
            StoryModuleTiming timing,
            StorySession session,
            CancellationToken ct)
        {
            if (line.Modules == null || line.Modules.Count == 0)
                return;

            for (int i = 0; i < line.Modules.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                StoryModuleSO module = line.Modules[i];
                if (module == null || module.Timing != timing)
                    continue;

                // Choice는 Runner가 따로 처리하므로 여기서는 제외
                if (module is StoryChoiceModuleSO)
                    continue;

                IStoryModuleExecutor executor = FindExecutor(module);
                if (executor == null)
                {
                    Debug.LogWarning(
                        $"No executor found for module '{module.name}' ({module.GetType().Name}).",
                        module);
                    continue;
                }

                await executor.ExecuteAsync(module, session, ct);
            }
        }

        private async UniTask ExecuteInlineModulesAsync(
            StoryLineSO line,
            StoryModuleTiming timing,
            StorySession session,
            CancellationToken ct)
        {
            if (line.InlineModules == null || line.InlineModules.Count == 0)
                return;

            for (int i = 0; i < line.InlineModules.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                StoryInlineModuleData module = line.InlineModules[i];
                if (module == null || module.Timing != timing)
                    continue;

                IStoryInlineModuleExecutor executor = FindInlineExecutor(module);
                if (executor == null)
                {
                    Debug.LogWarning(
                        $"No inline executor found for module '{module.DisplayName}' ({module.GetType().Name}).");
                    continue;
                }

                await executor.ExecuteAsync(module, session, ct);
            }
        }

        private IStoryModuleExecutor FindExecutor(StoryModuleSO module)
        {
            for (int i = 0; i < _executors.Count; i++)
            {
                if (_executors[i] != null && _executors[i].CanExecute(module))
                    return _executors[i];
            }
            return null;
        }

        private IStoryInlineModuleExecutor FindInlineExecutor(StoryInlineModuleData module)
        {
            for (int i = 0; i < _inlineExecutors.Count; i++)
            {
                if (_inlineExecutors[i] != null && _inlineExecutors[i].CanExecute(module))
                    return _inlineExecutors[i];
            }
            return null;
        }
    }
}
