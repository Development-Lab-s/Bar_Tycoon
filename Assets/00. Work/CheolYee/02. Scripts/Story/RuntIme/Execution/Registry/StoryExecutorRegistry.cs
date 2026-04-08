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
    public sealed class StoryExecutorRegistry : MonoBehaviour, IStoryExecutorRegistry
    {
        [SerializeField] private List<MonoBehaviour> executorSources = new();

        private readonly List<IStoryModuleExecutor> _executors = new();

        private void Awake()
        {
            _executors.Clear();

            for (int i = 0; i < executorSources.Count; i++)
            {
                if (executorSources[i] is IStoryModuleExecutor executor)
                {
                    _executors.Add(executor);
                }
                else if (executorSources[i] != null)
                {
                    Debug.LogError(
                        $"'{executorSources[i].GetType().Name}' does not implement IStoryModuleExecutor.",
                        executorSources[i]);
                }
            }
        }

        public async UniTask ExecuteModulesAsync(
            StoryLineSO line,
            StoryModuleTiming timing,
            StorySession session,
            CancellationToken ct)
        {
            if (line == null || session == null || line.Modules == null || line.Modules.Count == 0)
                return;

            for (int i = 0; i < line.Modules.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                StoryModuleSO module = line.Modules[i];
                if (module == null)
                    continue;

                if (module.Timing != timing)
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

        private IStoryModuleExecutor FindExecutor(StoryModuleSO module)
        {
            for (int i = 0; i < _executors.Count; i++)
            {
                if (_executors[i] != null && _executors[i].CanExecute(module))
                    return _executors[i];
            }

            return null;
        }
    }
}