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
        [SerializeField] private CockTailSlotSos allCockTailType;
        
        [Header("Sos")]
        [SerializeField] private EventChannelSO codexChaanelSo;
        [SerializeField] private SaveFileNameSO saveFileNameSo;
        
        public List<CockTailSlotSo> UnlockedCockTails { get; private set; } = new();

        private readonly CockTailSaveIds _saveData = new();
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
            CockTailSaveIds cockTailSaveIds = _saver.Load<CockTailSaveIds>();

            if (cockTailSaveIds == null)
            {
                Debug.LogWarning($"{saveFileNameSo.SavePath} 데이터 없음");
                return;
            }
            foreach (int cockTailSo in cockTailSaveIds.cockTailId)
            {
                CockTailSlotSo cockTail =
                    allCockTailType.cockTailSlotList.FirstOrDefault(x => x.CockTailId == cockTailSo);

                if (cockTail != null)
                {
                    _saveData.cockTailId.Add(cockTail.CockTailId);
                    UnlockedCockTails.Add(cockTail);
                }
            }
            OnAddCockTail?.Invoke(UnlockedCockTails);
        }

        private void HandleCockTailAdd(CockTailAddEvent obj)
        {
            int cockTailId = obj.cockTailSlotSo.CockTailId;

            if (_saveData.cockTailId.Contains(cockTailId))
            {
                Debug.Log("칵테일 도감 중복 무시");
                return;
            }
            
            UnlockedCockTails.Add(obj.cockTailSlotSo);
            OnAddCockTail?.Invoke(UnlockedCockTails);
            
            _saveData.cockTailId.Add(cockTailId);
            _saver.Save(_saveData);
        }
    }
}