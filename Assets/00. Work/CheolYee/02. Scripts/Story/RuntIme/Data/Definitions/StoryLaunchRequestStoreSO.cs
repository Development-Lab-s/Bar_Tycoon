using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    [CreateAssetMenu(fileName = "StoryLaunchRequestStore", menuName = "Story/Story Launch Request Store")]
    public sealed class StoryLaunchRequestStoreSO : ScriptableObject
    {
        [SerializeField] private string pendingEpisodeId;

        public void SetPendingEpisode(string episodeId)
        {
            pendingEpisodeId = episodeId;
        }

        public bool TryConsume(out string episodeId)
        {
            if (string.IsNullOrEmpty(pendingEpisodeId))
            {
                episodeId = null;
                return false;
            }
            episodeId = pendingEpisodeId;
            Clear();
            return true;
        }

        public void Clear()
        {
            pendingEpisodeId = string.Empty;
        }
    }
}
