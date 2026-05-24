using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using LitMotion;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "CharItemSO", menuName = "CharLike/CharItemSO")]
public class CharItemSO : ScriptableObject
{
    public CharacterEnum CharacterEnum;
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public string Ownercharacter { get; private set; }
    [field: SerializeField] public int Price { get; private set; }
    [field: SerializeField] public int LikePlus { get; private set; }
    public UnityEvent OnChangedCount = new();
}

