namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public interface IAbstructContractPopUp
    {
        void Close();
        void OnDisable();
        void Open(bool isAutoClose = false);
    }
}