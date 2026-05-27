using _00._Work._Resources._02._Scripts.Modules;
using BBJ.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public class ContractChat : AbstructContractPopUp, IAgentUI, IModule
    {
        private TextMeshPro _text;
        private ModuleOwner _owner;

        public bool IsOpen { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _text = GetComponentInChildren<TextMeshPro>();
        }

        public override void OnOpen() { }

        public UniTask OpenAsync()
        {
            gameObject.SetActive(true);
           
            return UniTask.CompletedTask;
        }

        public UniTask CloseAsync()
        {
            gameObject.SetActive(false);
           
            return UniTask.CompletedTask;
        }

        public void Message(string message)
        {
            _text.text = message;
        }

    }
}
