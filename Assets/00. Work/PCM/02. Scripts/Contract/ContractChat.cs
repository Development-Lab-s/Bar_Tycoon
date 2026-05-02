using _00._Work._Resources._02._Scripts.Modules;
using TMPro;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public class ContractChat : AbstructContractPopUp
    {
        [SerializeField] private ChatSO chat;
        private TextMeshPro _text;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _text = GetComponentInChildren<TextMeshPro>();
        }
        public override void Open(bool isAutoClose = false)
        {
            var a = chat.Line[Random.Range(0, chat.Line.Count)];
            _text.text = a;
            base.Open(isAutoClose);
        }

    }
}