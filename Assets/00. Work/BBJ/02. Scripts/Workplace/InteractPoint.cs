using System;
using UnityEngine;

namespace BBJ.WorkplaceSystem
{
    [Serializable]
    public struct InteractPoint
    {
        public Vector2Int Offset;
        public InteractRoleSO Role;
    }
}
