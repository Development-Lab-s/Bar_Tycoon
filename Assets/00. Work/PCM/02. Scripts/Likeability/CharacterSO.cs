using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSO", menuName = "CharLike/CharacterSO")]
public class CharacterSO : ScriptableObject
{
    public int currentLevel = 1;
    public int currentExp;
    public int maxLevel = 100;
    [System.Serializable]
    public struct DialogueData
    {
        [TextArea(2, 5)] public string context;
        //public Sprite characterFace;
    }

    [System.Serializable]
    public struct LevelDialogueGroup
    {
        public int unlockLevel;
        public DialogueData[] dialogues;
    }

    [Header("기본 정보")]
    public string characterName;
    public Sprite characterPortrait;

    [Header("레벨별 해금 대사 리스트 (원하는 만큼 추가 가능)")]
    public List<LevelDialogueGroup> dialogueGroups = new List<LevelDialogueGroup>();

    public List<DialogueData> GetAvailableDialogues(int currentLevel)
    {
        List<DialogueData> availableList = new List<DialogueData>();

        foreach (var group in dialogueGroups)
        {
            if (currentLevel >= group.unlockLevel)
            {
                availableList.AddRange(group.dialogues);
            }
        }

        return availableList;
    }

    public int GetMaxExpForLevel(int level)
    {
        return level * 50;
    }
}

