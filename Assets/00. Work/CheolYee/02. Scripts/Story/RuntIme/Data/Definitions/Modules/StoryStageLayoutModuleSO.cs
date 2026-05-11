using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
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

        [Header("Camera Focus")]
        [Tooltip("ActorInstanceKey of the actor the camera follows during this line. Empty means no camera movement.")]
        [SerializeField] private string cameraFocusTarget = "";

        [Header("Camera Timeline (optional)")]
        [Tooltip("Optional camera defaults and per-line keyframes. cameraFocusTarget remains as fallback for older lines.")]
        [SerializeField] private StoryCameraTrackData cameraTrack = new();

        [Header("Per-Line Timeline (optional)")]
        [Tooltip("Optional per-line background keyframe track for intra-line background changes.")]
        [SerializeField] private StoryBackgroundTrackData backgroundTrack = new();

        [Tooltip("Optional per-actor keyframe tracks for intra-line motion. " +
                 "Does not replace the actors snapshot. actorInstanceKey must match StoryActorStateData.actorInstanceKey.")]
        [SerializeField] private List<StoryActorTrackData> actorTracks = new();

        public StoryBackgroundStateData Background => background;
        public bool HasBackground => background is { HasBackground: true };
        public StoryBackgroundTrackData BackgroundTrack => backgroundTrack ??= new StoryBackgroundTrackData();
        public IReadOnlyList<StoryActorStateData> Actors => actors;
        public IReadOnlyList<StoryActorTrackData> ActorTracks => actorTracks;
        public string CameraFocusTarget => cameraFocusTarget ?? "";
        public StoryCameraTrackData CameraTrack => cameraTrack ??= new StoryCameraTrackData();

        public override string DisplayName => "Stage Layout";

        // Stage animation always starts concurrently with dialogue so motion and text sync.
        public override StoryModuleTiming Timing => StoryModuleTiming.WithDialogue;

#if UNITY_EDITOR
        public StoryBackgroundStateData BackgroundEditable => background;
        public StoryBackgroundTrackData BackgroundTrackEditable => backgroundTrack ??= new StoryBackgroundTrackData();
        public StoryCameraTrackData CameraTrackEditable => cameraTrack ??= new StoryCameraTrackData();
        public List<StoryActorStateData> ActorsEditable => actors;
        public List<StoryActorTrackData> ActorTracksEditable => actorTracks;
        public string CameraFocusTargetEditable { get => cameraFocusTarget ?? ""; set => cameraFocusTarget = value ?? ""; }
#endif

        private void OnValidate()
        {
            background ??= new StoryBackgroundStateData();
            backgroundTrack ??= new StoryBackgroundTrackData();
            cameraTrack ??= new StoryCameraTrackData();
            background.SyncBackgroundKey();

            if (actors == null)
                return;

            foreach (var actor in actors)
                actor?.SyncActorKey();
        }
    }
}
