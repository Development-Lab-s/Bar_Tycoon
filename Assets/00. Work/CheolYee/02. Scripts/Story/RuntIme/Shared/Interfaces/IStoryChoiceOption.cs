namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    /// <summary>
    /// 선택지 한 항목을 나타내는 최소 계약.
    /// StoryChoiceModuleSO.ChoiceOption 과 ChoiceInlineModuleData.ChoiceOption 이 모두 구현합니다.
    /// </summary>
    public interface IStoryChoiceOption
    {
        string OptionId            { get; }
        string Text                { get; }
        string ReactionStartLineId { get; }
        string ConvergeLineId      { get; }
    }
}
