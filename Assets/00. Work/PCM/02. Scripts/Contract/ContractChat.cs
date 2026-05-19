using _00._Work._Resources._02._Scripts.Modules;
using BBJ.UI;
using TMPro;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public class ContractChat : AbstructContractPopUp, IAgentUI
    {
        [SerializeField] private ChatSO chat;
        private TextMeshPro _text;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _text = GetComponentInChildren<TextMeshPro>();
        }

        public override void OnOpen()
        {
            var a = chat.Line[Random.Range(0, chat.Line.Count)];
            _text.text = a;
        }
    }
}