using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using LitMotion;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "CharItemSO", menuName = "CharLike/CharItemSO")]
public class CharItemSO : ScriptableObject
{
    public CharacterEnum CharacterEnum;
    [field:SerializeField]public string ItemName { get; private set; }
    [field: SerializeField] public string Ownercharacter { get; private set; }
    [field: SerializeField] public int MaxCount { get; private set; } = 99;
    [field: SerializeField]public int CurrentCount { get; set; }
    [field: SerializeField] public int LikePlus { get; private set; }
    public UnityEvent OnChangedCount = new();

        public void SetCount(int value)
        {
            CurrentCount = Mathf.Clamp(value, 0, MaxCount);
            OnChangedCount?.Invoke();

            SaveManager.Save(
                GetSaveData(),
                $"{ItemName}.save",
                "Items");
        }
    public void AddCount(int amount = 1)
    {
        SetCount(CurrentCount + amount);
    }
    public void RemoveCount(int amount = 1)
    {
        SetCount(CurrentCount - amount);
    }
    public CharItemSaveData GetSaveData()
    {
        return new CharItemSaveData
        {
            itemName = ItemName,
            currentCount = CurrentCount
        };
    }

    public void LoadSaveData(CharItemSaveData saveData)
    {
        CurrentCount = saveData.currentCount;

        OnChangedCount?.Invoke();
    }
}

