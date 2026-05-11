using UnityEngine;

namespace BBJ.Actions
{
    public interface IAgentPathProvider
    {
        void SetMoveDestination(Vector3 destination);
    }
}
