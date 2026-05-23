using System;
using _00._Work.Lusaload._02._Scripts.SO;
using BBJ.GridSystem.Objects;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex
{
    public class FurnitureSlotUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image objectImage;
     
        public event Action<ObjectDataSO> OnClickBtn;
        
        private ObjectDataSO _objectSo;

        private void Awake()
        {
            objectImage.gameObject.SetActive(false);
        }

        public void InputSO(ObjectDataSO objectSlotSo)
        {
            objectImage.gameObject.SetActive(true);
            _objectSo = objectSlotSo;
            objectImage.sprite = objectSlotSo.Icon;
        }
        
        public void Clear()
        {
            _objectSo = null;
            objectImage.sprite = null;
            objectImage.gameObject.SetActive(false);
        }

        public void BtnClick()
        {
            if (_objectSo != null)
            {
                OnClickBtn?.Invoke(_objectSo);   
            }
        }
    }
}