using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    [CreateAssetMenu(fileName = "WaitModule", menuName = "Story/Modules/Wait")]
    public sealed class StoryWaitModuleSO : StoryModuleSO
    {
        [Header("Wait")]
        [SerializeField, Min(0f)] private float duration = 0.4f;
        [SerializeField] private bool useUnscaledTime = true;

        public float Duration => duration;
        public bool UseUnscaledTime => useUnscaledTime;

        public override string DisplayName => "Wait";
    }
}