using System;
using UnityEngine;

[Serializable]
public class CharlikeabilitySave
{
    public void LoadSaveData(CharlikeabilitySave saveData)
    {
        currentLevel = saveData.currentLevel;
        currentExp = saveData.currentExp;
        maxLevel = saveData.maxLevel;
    }
    public string id;
    public string characterName;
    public int currentLevel;
    public int currentExp;
    public int maxLevel;
}
