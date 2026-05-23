using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.RuntimeModules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core
{
    public sealed class StorySession
    {
        public StoryEpisodeSO Episode { get; private set; }
        public string CurrentLineId { get; private set; }

        public StoryAdvanceMode AdvanceMode { get; set; } = StoryAdvanceMode.Manual;
        public bool IsUiHidden { get; set; }

        public readonly List<StoryLogEntry> Logs = new();
        public readonly Dictionary<string, string> ChoiceResults = new();
        public readonly Dictionary<string, ActorRuntimeHandle> ActiveActors = new();

        public void Begin(StoryEpisodeSO episode)
        {
            Episode = episode;
            CurrentLineId = episode != null ? episode.EntryLineId : string.Empty;

            AdvanceMode = StoryAdvanceMode.Manual;
            IsUiHidden = false;

            Logs.Clear();
            ChoiceResults.Clear();
            ActiveActors.Clear();
        }

        public void MoveTo(string nextLineId)
        {
            CurrentLineId = nextLineId;
        }
    }
}
