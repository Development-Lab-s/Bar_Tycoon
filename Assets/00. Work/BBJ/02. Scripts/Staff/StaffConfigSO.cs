using UnityEngine;
using UnityEngine.Timeline;

namespace BBJ.Staff
{
    [CreateAssetMenu(fileName = "StaffConfig", menuName = "Tycoon/Staff/StaffConfig")]
    public class StaffConfigSO : ScriptableObject
    {
        public AgentRole     Role;
        public GameObject    Prefab;
    }
}
