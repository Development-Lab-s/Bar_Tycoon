using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Execution.Registry
{
    public sealed class PassthroughStoryExecutorRegistry : MonoBehaviour, IStoryExecutorRegistry
    {
        public UniTask ExecuteModulesAsync(
            StoryLineSO line,
            StoryModuleTiming timing,
            StorySession session,
            CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
    }
}