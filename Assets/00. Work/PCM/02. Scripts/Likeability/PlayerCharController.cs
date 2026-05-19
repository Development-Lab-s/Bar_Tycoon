using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerCharController : MonoBehaviour, IPlayerCharController
{
    [Header("연동할 SO (인스펙터에서 할당)")]
    [field: SerializeField] public CharacterSO characterData { get; private set; }
    [field: SerializeField] public CharacterRegisterSO registerSO { get; private set; }

    [Header("현재 실시간 상태 정보")]


    [Header("연동할 UI (선택 사항)")]
    public Slider expSlider { get; private set; }

    public Image characterImage { get; private set; }
    public Action<int> levelTrigger { get; private set; }

    private void OnEnable()
    {
        if (registerSO != null) registerSO.Register(this);
        levelTrigger += GiveItem;
    }

    private void OnDisable()
    {
        if (registerSO != null) registerSO.Unregister(this);
    }

    private void Start()
    {
        UpdateUI();
    }

    public float GetExpRatio()
    {
        if (characterData.currentLevel >= characterData.maxLevel) return 1f;
        int maxExp = characterData.GetMaxExpForLevel(characterData.currentLevel);
        return (float)characterData.currentExp / maxExp;
    }
    public void GiveItem(int expAmount)
    {
        if (characterData.currentLevel >= characterData.maxLevel) return;

        characterData.currentExp += expAmount;
        Debug.Log($"{characterData.characterName} 호감도 경험치 +{expAmount}");

        bool isLeveledUp = false;

        // 레벨업 체크
        while (characterData.currentLevel < characterData.maxLevel && characterData.currentExp >= characterData.GetMaxExpForLevel(characterData.currentLevel))
        {
            characterData.currentExp -= characterData.GetMaxExpForLevel(characterData.currentLevel);
            characterData.currentLevel++;
            isLeveledUp = true;
            Debug.Log($"레벨업! 현재 레벨: {characterData.currentLevel}");
        }

        if (isLeveledUp)
        {
            PlayLevelUpDialogue();
        }

        UpdateUI();
    }

    private void PlayLevelUpDialogue()
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

        }
    }

    private void UpdateUI()
    {
        if (expSlider != null)
        {
            expSlider.value = GetExpRatio();
        }
    }
}
