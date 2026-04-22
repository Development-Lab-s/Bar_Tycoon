using Gamelib.EventSystem;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events
{
    public sealed class StoryChoiceCommitted : GameEvent
    {
        public string EpisodeId { get; }
        public string ChoiceId { get; }
        public string OptionId { get; }

        public StoryChoiceCommitted(string episodeId, string choiceId, string optionId)
        {
            EpisodeId = episodeId;
            ChoiceId = choiceId;
            OptionId = optionId;
        }
    }
}