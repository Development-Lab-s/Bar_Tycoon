using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core
{
    public sealed class StoryPrototypeStarter : MonoBehaviour
    {
        [SerializeField] private StoryService storyService;
        [SerializeField] private StoryEpisodeSO episode;
        [SerializeField] private bool playOnStart = false;

        private async UniTaskVoid Start()
        {
            if (!playOnStart || storyService == null || episode == null)
                return;

            await storyService.PlayAsync(new StoryPlayRequest(episode));
        }
    }
}