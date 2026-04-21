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
        public bool CanExecute(StoryModuleSO module) => module is StoryWaitModuleSO;

        public async UniTask ExecuteAsync(StoryModuleSO module, StorySession session, CancellationToken ct)
        {
            if (module is not StoryWaitModuleSO waitModule)
                return;

            await WaitAsync(waitModule.Duration, waitModule.UseUnscaledTime, ct);
        }

        private static async UniTask WaitAsync(float duration, bool useUnscaledTime, CancellationToken ct)
        {
            float d = Mathf.Max(0f, duration);
            if (d <= 0f)
                return;

            DelayType delayType = useUnscaledTime
                ? DelayType.UnscaledDeltaTime
                : DelayType.DeltaTime;

            await UniTask.Delay(
                TimeSpan.FromSeconds(d),
                delayType,
                PlayerLoopTiming.Update,
                ct);
        }
    }
}
