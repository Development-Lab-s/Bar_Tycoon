using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex
{
    public class CockTailContent : MonoBehaviour
    {
        private List<CockTailSlotUI>  _slots;
        
        public event Action<CockTailSlotSo>  OnClickBtn; 
        
        private void Awake()
        {
            _slots = GetComponentsInChildren<CockTailSlotUI>().ToList();

            foreach (CockTailSlotUI slot in _slots)
            {
                slot.OnClickBtn += HandleOnClick;
            }
        }

        private void OnDestroy()
        {
            foreach (CockTailSlotUI slot in _slots)
            {
                slot.OnClickBtn -= HandleOnClick;
            }
        }
        
        public void SetView(List<CockTailSlotSo> cockTailList)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < cockTailList.Count)
                {
                    _slots[i].InputSO(cockTailList[i]);
                }
                else
                {
                    _slots[i].Clear();
                }
            }
        }

        private void HandleOnClick(CockTailSlotSo obj)
        {
            OnClickBtn?.Invoke(obj);
        }
    }
}