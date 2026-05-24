using Cysharp.Threading.Tasks;

namespace BBJ.UI
{
    public interface IAgentUI
    {
        bool IsOpen { get; }
        UniTask OpenAsync();
        UniTask CloseAsync();
    }
}
