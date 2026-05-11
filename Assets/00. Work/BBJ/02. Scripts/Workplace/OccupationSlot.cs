using System;
using UnityEngine;

namespace BBJ.WorkplaceSystem
{
    public readonly struct OccupationSlot
    {
        public readonly Transform Owner;
        public readonly Action<Vector3> OnPositionChanged;
        public readonly Action OnProcessed;

        public OccupationSlot(
            Transform owner,
            Action<Vector3> onPositionChanged = null,
            Action onProcessed = null)
        {
            Owner = owner;
            OnPositionChanged = onPositionChanged;
            OnProcessed = onProcessed;
        }

        public void NotifyPosition(Vector3 worldPos) => OnPositionChanged?.Invoke(worldPos);
        public void NotifyProcessed() => OnProcessed?.Invoke();
    }
}
