using UnityEngine;

namespace BBJ.GridSystem.Pathfind
{
    public interface IPathRequestManager
    {
        void FinishedProcessingPath(Vector3[] path, bool success);
        void RequestPath(Vector3 pathStart, Vector3 pathEnd, PathRequestManager.PathRequestAction callback);
    }
}