using Gamelib.EventSystem;
using BBJ.GridSystem.Pathfind;

namespace BBJ.EventSystem
{
    public sealed class PathRequestEvent : GameEvent
    {
        public PathRequestManager.PathRequest PathRequest { get; private set; }
        public PathRequestEvent Init(PathRequestManager.PathRequest pathRequest)
        {
            PathRequest = pathRequest;
            return this;
        }
    }
}