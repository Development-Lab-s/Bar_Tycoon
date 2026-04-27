using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    [StoryModuleMetadata("TestModule", category: "Test", accentColorHex: "#5B9BD5", sortPriority: 20)]
    //[CreateAssetMenu(fileName = "TestModule", menuName = "Story/Modules/TestStoryModule", order = 0)]
    public class TestStoryModuleSO : StoryModuleSO
    {
        [Header("Test")]
        [SerializeField, Min(0f)] private float testFloat = 0.4f;
        [SerializeField] private bool useUnscaledTime = true;
        
        public float TestFloat => testFloat;
        public bool  UseUnscaledTime => useUnscaledTime;
        public override string DisplayName => "Test Module";
    }
}