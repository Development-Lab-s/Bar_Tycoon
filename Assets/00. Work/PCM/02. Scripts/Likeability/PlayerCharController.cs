using _00._Work._Resources._02._Scripts.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerCharController : MonoBehaviour, IPlayerCharController , IModule , IPlayer
{
    [Header("연동할 SO (인스펙터에서 할당)")]
    [field: SerializeField] public CharacterSO characterData { get; private set; }
    [field: SerializeField] public CharacterRegisterSO registerSO { get; private set; }
    [field: SerializeField]public CharcterLikeSO CharlikeSo{ get; set; }
    [field:SerializeField]public  UnityEvent<string> ChatOpen { get; set; }

    [Header("현재 실시간 상태 정보")]


    [Header("연동할 UI (선택 사항)")]
    public Image characterImage { get; private set; }
    public Action<int> levelTrigger { get; private set; }


    private ModuleOwner _owner;
    private int maxExp;

    public void Initialize(ModuleOwner owner)
    {
        _owner = owner;
    }

    private void OnEnable()
    {
        if (registerSO != null) registerSO.Register(this);
        levelTrigger += GiveItem;
    }

    private void OnDisable()
    {
        if (registerSO != null) registerSO.Unregister(this);
    }
    public float GetExpRatio()
    {
        float ratio = characterData.GetTotalProgressRatio();
        return ratio ;
    }
    public void GiveItem(int expAmount)
    {
        if (characterData.currentLevel >= characterData.maxLevel) return;

        characterData.currentExp += expAmount;
        Debug.Log($"{characterData.characterName} 호감도 경험치 +{expAmount}");
        int text = Random.Range(0, CharlikeSo.DialogueDataList.Count);
        ChatOpen?.Invoke(CharlikeSo.DialogueDataList[text].context);
        //bool isLeveledUp = false;
        // 레벨업 체크
        while (characterData.currentLevel < characterData.maxLevel && characterData.currentExp >= characterData.GetMaxExpForLevel(characterData.currentLevel))
        {
            characterData.currentExp -= characterData.GetMaxExpForLevel(characterData.currentLevel);
            characterData.currentLevel++;
            //isLeveledUp = true;
            Debug.Log($"레벨업! 현재 레벨: {characterData.currentLevel}");
        }

        //if (isLeveledUp)
        //{
        //    PlayLevelUpDialogue();
        //}
}

    public string PlayClickUpDialogue()
    {
        List<CharacterSO.DialogueData> availableDialogues = characterData.GetAvailableDialogues(characterData.currentLevel);

        if (availableDialogues != null && availableDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, availableDialogues.Count);
            CharacterSO.DialogueData selectedDialogue = availableDialogues[randomIndex];

            Debug.Log($"[{characterData.characterName} Lv.{characterData.currentLevel} 대사]: {selectedDialogue.context}");

            //if (selectedDialogue.characterFace != null && characterImage != null)
            //{
            //    characterImage.sprite = selectedDialogue.characterFace;
            //}
            return selectedDialogue.context;
        }
        return "";
    }
}
