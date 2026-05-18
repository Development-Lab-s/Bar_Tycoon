using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem.Handlers
{
    public abstract class TipCalculatorSO : ScriptableObject
    {
        public abstract int Calculate(ModuleOwner executor);
    }
}
