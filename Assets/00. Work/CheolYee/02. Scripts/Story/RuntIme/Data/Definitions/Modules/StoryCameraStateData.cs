using System;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    [Serializable]
    public sealed class StoryCameraStateData
    {
        [Tooltip("Stage local world position. (0,0)=StageRoot center. (2,0)=2 world units right. Unclamped.")]
        public Vector2 stageLocalPosition = Vector2.zero;

        [Tooltip("1 = 기본 orthographic size. 1.2 = 20% 확대 (finalOrtho = baseOrtho / zoom). 0 이하 불가.")]
        [Min(0.01f)]
        public float zoom = 1f;

        // ── Legacy fields: data preserved, not used in runtime path ──────────
        [HideInInspector] public string targetActorInstanceKey = "";
        [HideInInspector] public StoryCameraFollowMode followMode = StoryCameraFollowMode.FollowActor;
        [HideInInspector] public StoryCameraMoveMode moveMode = StoryCameraMoveMode.Smooth;
        [HideInInspector] public Vector2 normalizedOffset = Vector2.zero;
        [HideInInspector] public float zoomMultiplier = 1f;
        [HideInInspector] public Vector2 snapshotNormalizedPosition = new Vector2(0.5f, 0.5f);

        public StoryCameraStateData ShallowClone() => (StoryCameraStateData)MemberwiseClone();
    }
}
