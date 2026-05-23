using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Lusaload._02._Scripts.SO;
using BBJ.GridSystem.Objects;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex
{
    public class FurnitureContent : MonoBehaviour
    {
        [SerializeField] private FurnitureSlotUI cockTailSlotPrefab;
        
        private List<FurnitureSlotUI>  _slots = new();
        
        public event Action<ObjectDataSO>  OnClickBtn; 
        
        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            foreach (FurnitureSlotUI slot in _slots)
            {
                slot.OnClickBtn -= HandleOnClick;
            }
        }

        private void Init()
        {
            _slots = GetComponentsInChildren<FurnitureSlotUI>().ToList();

            foreach (FurnitureSlotUI slot in _slots)
            {
                slot.OnClickBtn += HandleOnClick;
            }
        }
        
        public void SetView(IReadOnlyCollection<ObjectDataSO> objectDataList)
        {
            List<ObjectDataSO> list = objectDataList.ToList();
            
            while (objectDataList.Count > _slots.Count)
            {
                FurnitureSlotUI cockTailSlot = Instantiate(cockTailSlotPrefab, transform);
                cockTailSlot.OnClickBtn += HandleOnClick;
                _slots.Add(cockTailSlot);
            }
            
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < list.Count)
                {
                    _slots[i].InputSO(list[i]);
                }
                else
                {
                    _slots[i].Clear();
                }
            }
        }

        private void HandleOnClick(ObjectDataSO obj)
        {
            OnClickBtn?.Invoke(obj);
        }
    }
}