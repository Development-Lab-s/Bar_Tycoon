using TMPro;
using UnityEngine;

public class ContractChat : AbstructContractPopUp
{
    [SerializeField]private ChatSO chat;
    private TextMeshPro _text;

    public override void Awake()
    {
        _text = GetComponentInChildren<TextMeshPro>();        
        base.Awake();
    }
    public override void Open(bool isAutoClose = false)
    {
        var a = chat.Line[Random.Range(0, chat.Line.Count)];
        _text.text = a;
        base.Open(isAutoClose);
    }

}
