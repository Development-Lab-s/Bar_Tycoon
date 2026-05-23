using _00._Work._Resources._02._Scripts.Modules;
using BBJ.UI;
using TMPro;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public class ContractChat : AbstructContractPopUp, IAgentUI , IModule , IAfterInitModule
    {
        private TextMeshPro _text;
        private ModuleOwner _owner;


        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _text = GetComponentInChildren<TextMeshPro>();
        }
        public void AfterInit()
        {
            _owner.GetModule<PlayerCharController>(); 
        }
        public override void OnOpen()
        {

        }
        public void Message(string message)
        {
            _text.text = message;
        }

    }
}