using BBJ.GridSystem.Pathfind;

namespace BBJ.Actions
{
    public interface IAgentPathProvider
    {
        RuntimeReference<IPathRequestManager> PathRequest { get; }
    }
}
