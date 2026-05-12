using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Work
{
    public abstract class WorkDurationSO : ScriptableObject
    {
        public abstract float GetDuration(ModuleOwner worker);
    }
}
