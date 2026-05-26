using System;
using Gamelib.EventSystem;
using Gamelib.SoundSystem;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors.CameraSystems
{
    /// <summary>
    /// Story 씬의 런타임 카메라를 식별하는 마커 컴포넌트.
    /// StoryStageCameraController가 씬 검색 시 이 컴포넌트로 카메라를 찾는다.
    /// FindObjectsInactive.Include로 검색하므로 비활성 상태에서도 탐색 가능.
    /// </summary>

    [DefaultExecutionOrder(-200)]
    public sealed class StoryRuntimeCameraMarker : MonoBehaviour
    {
        [SerializeField] private EventChannelSO soundChannel;

        private void Awake()
        {
            soundChannel.RaiseEvent(new StopSoundEvent(SoundChannelId.Bgm));
            soundChannel.RaiseEvent(new StopSoundEvent(SoundChannelId.StorySfx));
        }
    }
}
