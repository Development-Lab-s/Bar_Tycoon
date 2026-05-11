using System;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.WorkplaceSystem
{
    public struct WorkReservation
    {
        public ModuleOwner Worker;
        public Action OnCancelCallback;

        public WorkReservation(ModuleOwner worker, Action onCancel)
        {
            Worker           = worker;
            OnCancelCallback = onCancel;
        }

        public void Cancel() => OnCancelCallback?.Invoke();
    }
}
