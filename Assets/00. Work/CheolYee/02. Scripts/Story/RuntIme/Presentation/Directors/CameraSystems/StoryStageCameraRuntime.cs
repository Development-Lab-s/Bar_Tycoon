using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.Util;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.CameraSystems
{
    /// <summary>
    /// Camera application bridge for stage layout playback.
    ///
    /// This class owns camera focus/track/shake application for a sampled stage layout.
    /// The stage director should only decide when to apply camera samples, not how camera
    /// state is resolved or how actor focus targets are converted to camera movement.
    /// </summary>
    public sealed class StoryStageCameraRuntime : MonoBehaviour
    {
        [Header("Runtime Dependencies")]
        [SerializeField] private StoryStageCameraController cameraController;

        public StoryStageCameraMetrics ResolveStageReferenceMetrics()
        {
            return ResolveCameraController().ResolveStageReferenceMetrics();
        }

        public Vector3 GetCurrentCameraCenter()
        {
            StoryStageCameraController controller = ResolveCameraController();
            return controller != null ? controller.GetCurrentCameraCenter() : Vector3.zero;
        }

        public void ApplyCamera(
            StoryStageLayoutModuleSO layout,
            bool useCameraTrack,
            float previousTime,
            float currentTime,
            StoryStageTransitionRuntime transitionRuntime,
            Func<string, Vector2?> actorPivotResolver = null)
        {
            if (layout == null)
                return;

            if (useCameraTrack)
                ApplyCameraTrackSample(layout, currentTime, transitionRuntime, actorPivotResolver);
            else
                ApplyCameraDefault(layout);
        }

        /// <summary>
        /// Applies camera stageLocalPosition + zoom from defaultState.
        /// Camera does NOT auto-move to actors or speakers — only explicit stageLocalPosition drives movement.
        /// cameraFocusTarget on the layout is intentionally ignored here; actor tinting is handled separately.
        /// </summary>
        private void ApplyCameraDefault(StoryStageLayoutModuleSO layout)
        {
            StoryCameraStateData defaultState = layout.CameraTrack?.defaultState;
            if (defaultState == null)
                return;

            ResolveCameraController().ApplyStageCamera(defaultState);
        }

        private void ApplyCameraTrackSample(
            StoryStageLayoutModuleSO layout,
            float elapsed,
            StoryStageTransitionRuntime transitionRuntime,
            Func<string, Vector2?> actorPivotResolver)
        {
            if (transitionRuntime == null)
                return;

            StoryCameraStateData sample = transitionRuntime.SampleCameraTrack(layout?.CameraTrack, elapsed);
            if (sample == null)
                return;

            Vector2 targetContrib = StoryTransitionSampler.SampleCameraTargetContribution(
                layout?.CameraTrack, elapsed, actorPivotResolver);
            sample.stageLocalPosition += targetContrib;

            ResolveCameraController().ApplyStageCamera(sample);
        }

        private StoryStageCameraController ResolveCameraController()
        {
            StoryStageCameraController controller =
                StoryRuntimeComponentResolver.GetInSelfOrParentOrAdd(this, ref cameraController);

            controller?.Initialize();
            return controller;
        }

    }
}
