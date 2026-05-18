using BBJ.UI;

namespace BBJ.Modules
{
    public interface IAgentUIModule
    {
        void CloseAll();
        T Get<T>() where T : class, IAgentUI;
        void SetActiveUI<T>(bool isActive) where T : class, IAgentUI;
    }
}