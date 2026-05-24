using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Test
{
    public class CameraTest : MonoBehaviour
    {
        [SerializeField] private EventChannelSO  cameraEventChannelSO;
        [SerializeField] private List<Vector2> vec;
        [SerializeField] private List<Vector2> vec2;
        
        [ContextMenu("Test")]
        public void Test()
        {
            cameraEventChannelSO.RaiseEvent(new CameraManagerEvent().Init(vec, true));
            cameraEventChannelSO.RaiseEvent(new CameraManagerEvent().Init(vec2, true));
        }
    }
}