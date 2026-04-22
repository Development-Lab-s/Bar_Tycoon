using _00._Work._Resources._02._Scripts.Modules;
using System.Collections;
using UnityEngine;

namespace BBJ.Tycoon
{
    public abstract class WorkSO : ScriptableObject
    {
        public abstract WorkplaceType RequiredWorkplaceType { get; }

        public virtual bool CanExecute(Workplace workplace) => true;

        public abstract IEnumerator Execute(ModuleOwner owner, Workplace workplace);
    }
}
