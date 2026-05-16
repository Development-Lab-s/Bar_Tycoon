using System;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex
{
    public class CockTailSlotUI : MonoBehaviour
    {
        
        [Header("UI")]
        [SerializeField] private Image cockTailImage;
     
        public event Action<CocktailRecipeSO> OnClickBtn;
        
        private CocktailRecipeSO _cockTailSo;

        private void Awake()
        {
            cockTailImage.gameObject.SetActive(false);
        }

        public void InputSO(CocktailRecipeSO cockTailSlotSo)
        {
            cockTailImage.gameObject.SetActive(true);
            _cockTailSo = cockTailSlotSo;
            cockTailImage.sprite = _cockTailSo.cocktailIcon;
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