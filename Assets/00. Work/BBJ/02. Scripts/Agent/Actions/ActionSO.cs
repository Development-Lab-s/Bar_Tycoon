using _00._Work._Resources._02._Scripts.Systems.AnimationSystems;
using UnityEngine;

namespace BBJ.Actions
{
    [CreateAssetMenu(fileName = "Action data", menuName = "Tycoon/Action data")]
    public class ActionSO : ScriptableObject
    {
        public string      className;
        public AnimParamSO animParam;
    }
}
