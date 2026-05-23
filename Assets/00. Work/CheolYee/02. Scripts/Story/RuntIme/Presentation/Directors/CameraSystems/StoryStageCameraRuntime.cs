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
            StoryStageTransitionRuntime transitionRuntime)
        {
            if (layout == null)
                return;

            if (useCameraTrack)
                ApplyCameraTrackSample(layout, currentTime, transitionRuntime);
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
            StoryStageTransitionRuntime transitionRuntime)
        {
            if (transitionRuntime == null)
                return;

            StoryCameraStateData sample = transitionRuntime.SampleCameraTrack(
                layout?.CameraTrack,
                layout?.CameraFocusTarget,
                elapsed);

            if (sample == null)
                return;

            // stageLocalPosition / zoom 기반 단일 경로. FollowMode는 다음 Phase에서 구현.
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
