using _00._Work._Resources._02._Scripts.Modules;
using UnityEngine;
using UnityEngine.Events;

namespace _00._Work._Resources._02._Scripts.Agents
{
    public abstract class Agent : ModuleOwner
    {
        [field: SerializeField] public bool IsDead { get; set; }
        public AgentSensor Sensor { get; private set; }

        public UnityEvent onHit;
        public UnityEvent onDeath;

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            Sensor = GetModule<AgentSensor>();
            
            Debug.Assert(Sensor != null, $"{gameObject.name} 에 센서가 없습니다.");
        }

        protected virtual void Start()
        {
            
        }
    }
}