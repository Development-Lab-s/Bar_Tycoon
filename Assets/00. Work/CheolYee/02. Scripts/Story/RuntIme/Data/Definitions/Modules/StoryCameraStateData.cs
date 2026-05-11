using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    [Serializable]
    public sealed class StoryCameraStateData
    {
        [Tooltip("Actor instance key the camera targets. Empty means keep current framing.")]
        public string targetActorInstanceKey = "";

        [Tooltip("How the camera resolves target movement after the key/default state is applied.")]
        public StoryCameraFollowMode followMode = StoryCameraFollowMode.FollowActor;

        [Tooltip("How the camera reaches the new target when Camera Target changes.")]
        public StoryCameraMoveMode moveMode = StoryCameraMoveMode.Smooth;

        [Tooltip("Normalized camera offset relative to its target/reference frame.")]
        public Vector2 normalizedOffset = Vector2.zero;

        [Tooltip("1 = default orthographic size. Higher zooms in by shrinking the world width.")]
        [Min(0.01f)]
        public float zoomMultiplier = 1f;

        [Tooltip("Snapshot position used when followMode is SnapshotPosition.")]
        public Vector2 snapshotNormalizedPosition = new Vector2(0.5f, 0.5f);

        public StoryCameraStateData ShallowClone() => (StoryCameraStateData)MemberwiseClone();
    }
}
