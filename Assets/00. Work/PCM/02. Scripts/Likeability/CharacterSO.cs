using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSO", menuName = "CharLike/CharacterSO")]
public class CharacterSO : ScriptableObject
{
    [System.Serializable]
    public struct DialogueData
    {
        [TextArea(2, 5)] public string context;
        public Sprite characterFace;
        public AudioClip voiceClip;
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

    // [Helper 함수 1] 현재 레벨까지 '해금된 모든 대사'를 리스트로 묶어서 반환
    // 예: 현재 12레벨이면, 5레벨 그룹 + 10레벨 그룹의 대사가 모두 합쳐져서 반환됩니다.
    public List<DialogueData> GetAvailableDialogues(int currentLevel)
    {
        List<DialogueData> availableList = new List<DialogueData>();

        // 조건에 맞는 그룹들의 대사를 전부 하나로 합침
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

