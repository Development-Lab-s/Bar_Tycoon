using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSO", menuName = "CharLike/CharacterSO")]
public class CharacterSO : ScriptableObject
{
    public string id;
    public string characterName;
    public int maxLevel = 10;

    [System.Serializable]
    public struct DialogueData
    {
        [TextArea(2, 5)] public string context;
    }

    [System.Serializable]
    public struct LevelDialogueGroup
    {
        public int unlockLevel;
        public DialogueData[] dialogues;
    }

    [Header("기본 설정")]
    public Sprite characterPortrait;

    [Header("호감도 레벨별 대사 리스트 (원하는 만큼 추가 가능)")]
    public List<LevelDialogueGroup> dialogueGroups = new List<LevelDialogueGroup>();

    public List<DialogueData> GetAvailableDialogues(int currentLevel)
    {
        var availableList = new List<DialogueData>();
        foreach (var group in dialogueGroups)
        {
            if (currentLevel >= group.unlockLevel)
                availableList.AddRange(group.dialogues);
        }
        return availableList;
    }

    public int GetMaxExpForLevel(int level)
    {
        return (int)(Math.Pow(level - 1, 2) * 21.875 + 200);
    }

    public int GetTotalExpUpToLevel(int level)
    {
        int total = 0;
        for (int i = 1; i < level; i++)
            total += GetMaxExpForLevel(i);
        return total;
    }

    public float GetTotalProgressRatio(int currentLevel, int currentExp)
    {
        if (currentLevel >= maxLevel) return 1f;

        float levelBaseRatio = (float)(currentLevel - 1) / (maxLevel - 1);
        int maxExpForCurrentLevel = GetMaxExpForLevel(currentLevel);
        if (maxExpForCurrentLevel <= 0) return levelBaseRatio;
        float currentLevelExpRatio = (float)currentExp / maxExpForCurrentLevel;
        float expMicroRatio = currentLevelExpRatio / (maxLevel - 1);
        return levelBaseRatio + expMicroRatio;
    }
}
