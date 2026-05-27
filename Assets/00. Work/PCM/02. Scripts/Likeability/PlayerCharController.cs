using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.SaveSystem;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Events;
using Assets._00._Work.PCM._02._Scripts.Contract;
using Gamelib.EventSystem;
using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerCharController : MonoBehaviour, IPlayerCharController , IModule , IPlayer , IAfterInitModule
{
    [Header("������ SO (�ν����Ϳ��� �Ҵ�)")]
    [field: SerializeField] public CharacterSO characterData { get; private set; }
    [field: SerializeField] public CharacterRegisterSO registerSO { get; private set; }
    [field: SerializeField]public CharcterLikeSO CharlikeSo{ get; set; }
    [field:SerializeField]public  UnityEvent<string> ChatOpen { get; set; }
    [SerializeField] private EventChannelSO exitBtnClickEvent;
     private ContractChat chat;

    [Header("���� �ǽð� ���� ����")]


    [Header("������ UI (���� ����)")]
    public Image characterImage { get; private set; }
    public Action<int> levelTrigger { get; private set; }


    private ModuleOwner _owner;
    private int maxExp;

    [SerializeField]private EventChannelSO storyCommandChannel;
    [SerializeField]private StoryEpisodeSO[] storyEpisodeSo;

    public void Initialize(ModuleOwner owner)
    {
        _owner = owner;
    }
    public void AfterInit()
    {
        chat = _owner.GetModule<ContractChat>(); 
    }
    public void Awake()
    {
        //SaveManager.DeleteSave($"{characterData.id}.save", "Characters");
        if (!SaveManager.IsSaveFile($"{characterData.id}.save","Characters"))return;
        CharlikeabilitySave saveData =
            (CharlikeabilitySave)SaveManager.Load(
                typeof(CharlikeabilitySave),
                $"{characterData.id}.save",
                "Characters");
        characterData.LoadSaveData(saveData);
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
        Debug.Log(characterData.GetTotalProgressRatio());
        float ratio = characterData.GetTotalProgressRatio();
        return ratio ;
    }
    public void GiveItem(int expAmount)
    {
        if (characterData.currentLevel >= characterData.maxLevel) return;

        characterData.currentExp += expAmount;
        int text = Random.Range(0, CharlikeSo.DialogueDataList.Count);
        ChatOpen?.Invoke(CharlikeSo.DialogueDataList[text].context);
        //bool isLeveledUp = false;
        // ������ üũ
        while (characterData.currentLevel < characterData.maxLevel && characterData.currentExp >= characterData.GetMaxExpForLevel(characterData.currentLevel))
        {
            characterData.currentExp -= characterData.GetMaxExpForLevel(characterData.currentLevel);
            characterData.currentLevel++;
            CheckLikeStory();
            //isLeveledUp = true;
        }
        SaveManager.Save(characterData.GetSaveData(), $"{characterData.id}.save", "Characters");

        //if (isLeveledUp)
        //{
        //    PlayLevelUpDialogue();
        //}
    }

    public void PlayClickUpDialogue()
    {
        List<CharacterSO.DialogueData> availableDialogues = characterData.GetAvailableDialogues(characterData.currentLevel);

        if (availableDialogues != null && availableDialogues.Count > 0)
        {
            int randomIndex = Random.Range(0, availableDialogues.Count);
            CharacterSO.DialogueData selectedDialogue = availableDialogues[randomIndex];

            Debug.Log($"[{characterData.characterName} Lv.{characterData.currentLevel} ���]: {selectedDialogue.context}");

            chat.Message(selectedDialogue.context);
        }
    }
    private void CheckLikeStory()
    {
        if (characterData.currentLevel % 2 == 0)
        {
            if (((characterData.currentLevel / 2)-1 )>= storyEpisodeSo.Length) return;
            storyCommandChannel.RaiseEvent(new StoryEpisodeUnlockRequested(storyEpisodeSo[(characterData.currentLevel/2)-1]));
            exitBtnClickEvent.RaiseEvent(new LevelUpRewardeExitBtnClickEvent().Init());
        }
    }
}
