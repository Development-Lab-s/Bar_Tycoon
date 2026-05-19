using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex
{
    public class CockTailContent : MonoBehaviour
    {
        [SerializeField] private CockTailSlotUI cockTailSlotPrefab;
        
        private List<CockTailSlotUI>  _slots = new();
        
        public event Action<CocktailRecipeSO>  OnClickBtn; 
        
        private void Awake()
        {
            Init();
        }

        private void OnDestroy()
        {
            foreach (CockTailSlotUI slot in _slots)
            {
                slot.OnClickBtn -= HandleOnClick;
            }
        }

        private void Init()
        {
            _slots = GetComponentsInChildren<CockTailSlotUI>().ToList();

            foreach (CockTailSlotUI slot in _slots)
            {
                slot.OnClickBtn += HandleOnClick;
            }
        }
        
        public void SetView(HashSet<CocktailRecipeSO> cockTailList)
        {
            List<CocktailRecipeSO> list = cockTailList.ToList();
            
            while (cockTailList.Count > _slots.Count)
            {
                CockTailSlotUI cockTailSlot = Instantiate(cockTailSlotPrefab, transform);
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

        private void HandleOnClick(CocktailRecipeSO obj)
        {
            OnClickBtn?.Invoke(obj);
        }
    }
}