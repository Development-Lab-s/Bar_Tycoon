using System;
using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces.Executor;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Execution.Executors
{
    public sealed class WaitModuleExecutor : MonoBehaviour, IStoryModuleExecutor
    {
        public bool CanExecute(StoryModuleSO module)
        {
            return module is StoryWaitModuleSO;
        }

        public async UniTask ExecuteAsync(StoryModuleSO module, StorySession session, CancellationToken ct)
        {
            if (module is not StoryWaitModuleSO waitModule)
                return;

            float duration = Mathf.Max(0f, waitModule.Duration);
            if (duration <= 0f)
                return;

            DelayType delayType = waitModule.UseUnscaledTime
                ? DelayType.UnscaledDeltaTime
                : DelayType.DeltaTime;

            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                delayType,
                PlayerLoopTiming.Update,
                ct);
        }
    }
}