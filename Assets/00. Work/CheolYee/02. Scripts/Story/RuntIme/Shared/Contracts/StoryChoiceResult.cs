namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts
{
    /// 플레이어가 선택한 선택지의 결과를 나타내는 구조체입니다.
    public readonly struct StoryChoiceResult
    {
        public string ChoiceId { get; }
        public string OptionId { get; }
        public string DisplayText { get; }
        public string NextLineId { get; }

        public StoryChoiceResult(string choiceId, string optionId, string displayText, string nextLineId)
        {
            ChoiceId = choiceId;
            OptionId = optionId;
            DisplayText = displayText;
            NextLineId = nextLineId;
        }
    }
}