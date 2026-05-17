using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCharController : MonoBehaviour
{
    [Header("연동할 SO (인스펙터에서 할당)")]
    [field:SerializeField]public CharacterSO characterData { get; private set; }
    [field:SerializeField]public CharacterRegisterSO registerSO { get; private set; }

    [Header("현재 실시간 상태 정보")]
    public int currentLevel { get; private set; } = 1;
    public int currentExp { get; private set; } = 0;
    public int maxLevel { get; private set; } = 100;

    [Header("연동할 UI (선택 사항)")]
    public Slider expSlider { get; private set; }

    public Image characterImage { get; private set; }

    private void OnEnable()
    {
        if (registerSO != null) registerSO.Register(this);
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
        if (currentLevel >= maxLevel) return 1f;
        int maxExp = characterData.GetMaxExpForLevel(currentLevel);
        return (float)currentExp / maxExp;
    }
    public void GiveItem(int expAmount)
    {
        if (currentLevel >= maxLevel) return;

        currentExp += expAmount;
        Debug.Log($"{characterData.characterName} 호감도 경험치 +{expAmount}");

        bool isLeveledUp = false;

        // 레벨업 체크
        while (currentLevel < maxLevel && currentExp >= characterData.GetMaxExpForLevel(currentLevel))
        {
            currentExp -= characterData.GetMaxExpForLevel(currentLevel);
            currentLevel++;
            isLeveledUp = true;
            Debug.Log($"레벨업! 현재 레벨: {currentLevel}");
        }

        if (isLeveledUp)
        {
            PlayLevelUpDialogue();
        }

        UpdateUI();
    }

    private void PlayLevelUpDialogue()
    {
        List<CharacterSO.DialogueData> availableDialogues = characterData.GetAvailableDialogues(currentLevel);

        if (availableDialogues != null && availableDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, availableDialogues.Count);
            CharacterSO.DialogueData selectedDialogue = availableDialogues[randomIndex];

            Debug.Log($"[{characterData.characterName} Lv.{currentLevel} 대사]: {selectedDialogue.context}");

            // 표정 이미지가 등록되어 있고 UI가 연결되어 있다면 표정 변경
            if (selectedDialogue.characterFace != null && characterImage != null)
            {
                characterImage.sprite = selectedDialogue.characterFace;
            }
    
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
