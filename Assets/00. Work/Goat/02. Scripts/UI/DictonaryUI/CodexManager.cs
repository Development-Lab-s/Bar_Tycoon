using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI
{
    public class CodexManager : MonoBehaviour
    {
        [Header("All CockTail Type")]
        [SerializeField] private List<CockTailSlotSo> allCockTailType;
        
        [Header("Sos")]
        [SerializeField] private EventChannelSO codexChaanelSo;
        [SerializeField] private SaveFileNameSO saveFileNameSo;
        
        public List<CockTailSlotSo> UnlockedCockTails { get; private set; } = new();

        private readonly CockTailSaveNames _saveData = new();
        private JsonSaveService _saver;

        public event Action<List<CockTailSlotSo>> OnAddCockTail;

        private void Awake()
        {
            _saver = new JsonSaveService(saveFileNameSo);
            
            codexChaanelSo.AddListener<CockTailAddEvent>(HandleCockTailAdd);
        }

        private void Start()
        {
            LoadCockTailData();
        }

        private void OnDestroy()
        {
            codexChaanelSo.RemoveListener<CockTailAddEvent>(HandleCockTailAdd);
        }

        private void LoadCockTailData()
        {
            CockTailSaveNames cockTailSaveNames = _saver.Load<CockTailSaveNames>();

            if (cockTailSaveNames == null)
            {
                Debug.LogWarning($"{saveFileNameSo.SavePath} 데이터 없음");
                return;
            }
            foreach (string cockTailSo in cockTailSaveNames.cockTailName)
            {
                CockTailSlotSo cockTail =
                    allCockTailType.FirstOrDefault(x => x.CockTailName == cockTailSo);

                if (cockTail != null)
                {
                    _saveData.cockTailName.Add(cockTail.CockTailName);
                    UnlockedCockTails.Add(cockTail);
                }
            }
            OnAddCockTail?.Invoke(UnlockedCockTails);
        }

        private void HandleCockTailAdd(CockTailAddEvent obj)
        {
            string cockTailName = obj.cockTailSlotSo.CockTailName;

            if (_saveData.cockTailName.Contains(cockTailName))
            {
                Debug.Log("칵테일 도감 중복 무시");
                return;
            }
            
            UnlockedCockTails.Add(obj.cockTailSlotSo);
            OnAddCockTail?.Invoke(UnlockedCockTails);
            
            _saveData.cockTailName.Add(cockTailName);
            _saver.Save(_saveData);
        }
    }
}