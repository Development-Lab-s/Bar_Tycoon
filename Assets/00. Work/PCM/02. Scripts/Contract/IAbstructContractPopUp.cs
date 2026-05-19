using _00._Work._Resources._02._Scripts.Modules;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public interface IAbstructContractPopUp
    {
        bool isOpen { get; }
        void OnClose();
        void OnDisable();
        void Open(bool isAutoClose = false);
        bool IsAnimating { get; }
    }
}