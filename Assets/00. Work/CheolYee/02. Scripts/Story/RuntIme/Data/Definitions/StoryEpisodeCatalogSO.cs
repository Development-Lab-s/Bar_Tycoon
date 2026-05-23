using System;
using System.Collections.Generic;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    [CreateAssetMenu(fileName = "StoryEpisodeCatalog", menuName = "Story/Story Episode Catalog")]
    public sealed class StoryEpisodeCatalogSO : ScriptableObject
    {
        [SerializeField] private List<StoryEpisodeCatalogEntry> entries = new();

        public IReadOnlyList<StoryEpisodeCatalogEntry> Entries => entries;

        public bool TryGetEntry(string episodeId, out StoryEpisodeCatalogEntry entry)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Episode != null && entries[i].Episode.EpisodeId == episodeId)
                {
                    entry = entries[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }
    }

    [Serializable]
    public sealed class StoryEpisodeCatalogEntry
    {
        [SerializeField] private StoryEpisodeSO episode;
        [SerializeField] private Sprite thumbnail;

        public StoryEpisodeSO Episode => episode;
        public Sprite Thumbnail => thumbnail;
    }
}
