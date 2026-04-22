using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    /// <summary>
    /// Absolute stage state at a story line. The module is authored as a
    /// StoryLineSO.modules sub-asset and remains the source of truth for preview
    /// and runtime stage playback.
    /// </summary>
    [StoryModuleMetadata("Stage Layout", category: "Stage", accentColorHex: "#9B59B6", sortPriority: 1)]
    [CreateAssetMenu(fileName = "StageLayoutModule", menuName = "Story/Modules/Stage Layout")]
    public sealed class StoryStageLayoutModuleSO : StoryModuleSO
    {
        [Header("Background")]
        [Tooltip("Optional line-level background state. Empty means the previous background state is kept.")]
        [SerializeField] private StoryBackgroundStateData background = new();

        [Header("Actors")]
        [Tooltip("Absolute actor states visible at this line.")]
        [SerializeField] private List<StoryActorStateData> actors = new();

        public StoryBackgroundStateData Background => background;
        public bool HasBackground => background is { HasBackground: true };
        public IReadOnlyList<StoryActorStateData> Actors => actors;

        public override string DisplayName => "Stage Layout";

#if UNITY_EDITOR
        public StoryBackgroundStateData BackgroundEditable => background;
        public List<StoryActorStateData> ActorsEditable => actors;
#endif

        private void OnValidate()
        {
            background ??= new StoryBackgroundStateData();
            background.SyncBackgroundKey();

            if (actors == null)
                return;

            foreach (var actor in actors)
                actor?.SyncActorKey();
        }
    }
}
