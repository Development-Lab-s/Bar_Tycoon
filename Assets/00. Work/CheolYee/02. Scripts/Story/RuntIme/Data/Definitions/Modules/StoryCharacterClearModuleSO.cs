using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    [StoryModuleMetadata("Character Clear", category: "Character", accentColorHex: "#70AD47", sortPriority: 20)]
    [CreateAssetMenu(fileName = "CharacterClearModule", menuName = "Story/Modules/Character Clear")]
    public sealed class StoryCharacterClearModuleSO : StoryModuleSO
    {
        public override string DisplayName => "Character Clear";
    }
}
