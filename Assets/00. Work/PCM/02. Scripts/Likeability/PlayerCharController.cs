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

public class PlayerCharController : MonoBehaviour, IPlayerCharController, IModule, IPlayer, IAfterInitModule
{
    [Header("캐릭터 SO (인스펙터에서 할당)")]
    [field: SerializeField] public CharacterSO characterData { get; private set; }
    [field: SerializeField] public CharacterRegisterSO registerSO { get; private set; }
    [SerializeField] private CharacterRegisterSO sharedRegisterSO;
    [field: SerializeField] public CharcterLikeSO CharlikeSo { get; set; }
    [field: SerializeField] public UnityEvent<string> ChatOpen { get; set; }
    [SerializeField] private EventChannelSO exitBtnClickEvent;
    private ContractChat chat;

    [Header("런타임 상태 (플레이 모드에서 인스펙터 확인 가능)")]
    [SerializeField] private int _currentLevel = 1;
    [SerializeField] private int _currentExp = 0;

    public int CurrentLevel => _currentLevel;
    public int CurrentExp => _currentExp;

    [Header("캐릭터 UI (씬 참조)")]
    public Image characterImage { get; private set; }
    public Action<int> levelTrigger { get; private set; }

    private ModuleOwner _owner;

    [SerializeField] private EventChannelSO storyCommandChannel;
    [SerializeField] private StoryEpisodeSO[] storyEpisodeSo;

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
        if (!SaveManager.IsSaveFile($"{characterData.id}.save", "Characters")) return;
        var saveData = (CharlikeabilitySave)SaveManager.Load(
            typeof(CharlikeabilitySave),
            $"{characterData.id}.save",
            "Characters");
        if (saveData == null) return;
        _currentLevel = saveData.currentLevel;
        _currentExp = saveData.currentExp;
    }

    private void OnEnable()
    {
        if (registerSO != null) registerSO.Register(this);
        if (sharedRegisterSO != null) sharedRegisterSO.Register(this);
        levelTrigger += GiveItem;
    }

    private void OnDisable()
    {
        if (registerSO != null) registerSO.Unregister(this);
        if (sharedRegisterSO != null) sharedRegisterSO.Unregister(this);
    }

    public float GetExpRatio()
    {
        float ratio = characterData.GetTotalProgressRatio(_currentLevel, _currentExp);
        Debug.Log(ratio);
        return ratio;
    }

    public void GiveItem(int expAmount)
    {
        if (_currentLevel >= characterData.maxLevel) return;

        _currentExp += expAmount;
        int text = Random.Range(0, CharlikeSo.DialogueDataList.Count);
        ChatOpen?.Invoke(CharlikeSo.DialogueDataList[text].context);

        while (_currentLevel < characterData.maxLevel &&
               _currentExp >= characterData.GetMaxExpForLevel(_currentLevel))
        {
            _currentExp -= characterData.GetMaxExpForLevel(_currentLevel);
            _currentLevel++;
            CheckLikeStory();
        }

        SaveManager.Save(new CharlikeabilitySave
        {
            id = characterData.id,
            characterName = characterData.characterName,
            currentLevel = _currentLevel,
            currentExp = _currentExp,
            maxLevel = characterData.maxLevel
        }, $"{characterData.id}.save", "Characters");
    }

    public void PlayClickUpDialogue()
    {
        var availableDialogues = characterData.GetAvailableDialogues(_currentLevel);
        if (availableDialogues == null || availableDialogues.Count == 0) return;

        int randomIndex = Random.Range(0, availableDialogues.Count);
        Debug.Log($"[{characterData.characterName} Lv.{_currentLevel} 대사]: {availableDialogues[randomIndex].context}");
        chat.Message(availableDialogues[randomIndex].context);
    }

    private void CheckLikeStory()
    {
        if (_currentLevel % 2 == 0)
        {
            if ((_currentLevel / 2 - 1) >= storyEpisodeSo.Length) return;
            storyCommandChannel.RaiseEvent(new StoryEpisodeUnlockRequested(storyEpisodeSo[_currentLevel / 2 - 1]));
            exitBtnClickEvent.RaiseEvent(new LevelUpRewardeExitBtnClickEvent().Init());
        }
    }
}
