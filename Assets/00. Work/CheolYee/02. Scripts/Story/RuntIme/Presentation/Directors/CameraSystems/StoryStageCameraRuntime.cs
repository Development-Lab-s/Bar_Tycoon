using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.Util;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
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
        [SerializeField] private StoryActorStageRuntime actorRuntime;

        private bool _loggedMissingActorRuntime;

        public StoryStageCameraMetrics ResolveStageReferenceMetrics()
        {
            return ResolveCameraController().ResolveStageReferenceMetrics();
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
            {
                ApplyCameraTrackSample(layout, currentTime, transitionRuntime);
            }
            else
            {
                ApplyCameraFocusXOnly(layout);
            }
        }

        private void ApplyCameraFocusXOnly(StoryStageLayoutModuleSO layout)
        {
            string focusKey = layout?.CameraFocusTarget;
            if (string.IsNullOrWhiteSpace(focusKey))
                return;

            StoryActorStageRuntime actor = ResolveActorRuntime();
            if (actor == null)
                return;

            if (!actor.TryGetActorWorldPosition(focusKey, out Vector3 actorPosition))
            {
                Debug.LogWarning(
                    $"[{nameof(StoryStageCameraRuntime)}] cameraFocusTarget '{focusKey}' not found in actor runtime.",
                    this);

                return;
            }

            ResolveCameraController().FocusXSmooth(actorPosition.x);
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

            StoryStageCameraController controller = ResolveCameraController();
            controller.ApplyZoom(sample.zoomMultiplier);

            StoryStageCameraMetrics stage = controller.ResolveStageReferenceMetrics();

            float desiredX;
            if (sample.followMode == StoryCameraFollowMode.SnapshotPosition)
            {
                desiredX = stage.ActorPosition(sample.snapshotNormalizedPosition, Vector2.zero, 0f).x;
            }
            else if (!string.IsNullOrWhiteSpace(sample.targetActorInstanceKey)
                     && TryGetActorWorldPosition(sample.targetActorInstanceKey, out Vector3 actorPosition))
            {
                desiredX = actorPosition.x;
            }
            else
            {
                desiredX = controller.GetCurrentCameraCenter().x;
            }

            float desiredY = controller.StageReferenceCenter.y + sample.normalizedOffset.y * stage.Height;
            desiredX += sample.normalizedOffset.x * stage.Width;

            if (sample.moveMode == StoryCameraMoveMode.Instant)
                controller.MoveToImmediate(desiredX, desiredY);
            else
                controller.MoveToSmooth(desiredX, desiredY);
        }

        private bool TryGetActorWorldPosition(string actorInstanceKey, out Vector3 position)
        {
            position = default;

            StoryActorStageRuntime actor = ResolveActorRuntime();
            return actor != null && actor.TryGetActorWorldPosition(actorInstanceKey, out position);
        }

        private StoryStageCameraController ResolveCameraController()
        {
            StoryStageCameraController controller =
                StoryRuntimeComponentResolver.GetInSelfOrParentOrAdd(this, ref cameraController);

            controller?.Initialize();
            return controller;
        }

        private StoryActorStageRuntime ResolveActorRuntime()
        {
            return StoryRuntimeComponentResolver.GetInSelfOrParentWithWarning(
                this,
                ref actorRuntime,
                ref _loggedMissingActorRuntime,
                $"[{nameof(StoryStageCameraRuntime)}] {nameof(StoryActorStageRuntime)} is missing. Camera actor focus cannot be resolved.");
        }
    }
}
