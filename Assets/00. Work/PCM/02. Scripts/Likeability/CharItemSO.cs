using LitMotion;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "CharItemSO", menuName = "CharLike/CharItemSO")]
public class CharItemSO : ScriptableObject
{
    public CharacterEnum CharacterEnum;
    [field:SerializeField]public string ItemName { get; private set; }
    [field: SerializeField] public int MaxCount { get; private set; } = 99;
    [field: SerializeField]public int CurrentCount { get; set; }
    [field: SerializeField] public int LikePlus { get; private set; }

    public UnityEvent OnChangedCount { get; set; }

    public void SetCount(int value)
    {
        CurrentCount = Mathf.Clamp(value, 0, MaxCount);
        OnChangedCount?.Invoke();
    }
    public void AddCount(int amount = 1)
    {
        SetCount(CurrentCount + amount);
    }
    public void RemoveCount(int amount = 1)
    {
        SetCount(CurrentCount - amount);
    }
}
