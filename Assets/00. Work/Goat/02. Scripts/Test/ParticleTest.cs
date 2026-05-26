using _00._Work.Goat._02._Scripts.Events;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Test
{
    public class ParticleTest : MonoBehaviour
    {
        [SerializeField] private ParticleType particleType;
        [SerializeField] private EventChannelSO eventChannel;
        
        [ContextMenu("Start")]
        public void StartParticle()
        {
            eventChannel.RaiseEvent(new SungtaeParticleEvent().Init(particleType, Vector2.zero));
        }
    }
}