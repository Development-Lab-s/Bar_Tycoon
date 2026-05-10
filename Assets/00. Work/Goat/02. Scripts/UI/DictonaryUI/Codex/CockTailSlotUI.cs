using System;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex
{
    public class CockTailSlotUI : MonoBehaviour
    {
        
        [Header("UI")]
        [SerializeField] private Image cockTailImage;
     
        public event Action<CockTailSlotSo> OnClickBtn;
        
        private CockTailSlotSo _cockTailSo;

        private void Awake()
        {
            cockTailImage.gameObject.SetActive(false);
        }

        public void InputSO(CockTailSlotSo cockTailSlotSo)
        {
            cockTailImage.gameObject.SetActive(true);
            _cockTailSo = cockTailSlotSo;
            cockTailImage.sprite = _cockTailSo.CockTailImage;
        }
        
        public void Clear()
        {
            _cockTailSo = null;
            cockTailImage.sprite = null;
            cockTailImage.gameObject.SetActive(false);
        }

        public void BtnClick()
        {
            if (_cockTailSo != null)
            {
                OnClickBtn?.Invoke(_cockTailSo);   
            }
        }
    }
}