using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas
{
    public class ButtonCanvasChanger : MonoBehaviour
    {
            
        private List<ButtonCanvas> _buttonCanvases;
        
        private ButtonType _nowButtonType;

        public event Action<ButtonType> OnClickButton;
        
        private void Start()
        {
            Init();
        }

        private void OnDestroy()
        {
            foreach (ButtonCanvas buttonCanvas in _buttonCanvases)
            {
                buttonCanvas.OnClickButton -= HandleImageChange;
            }
        }

        private void Init()
        {
            _buttonCanvases = GetComponentsInChildren<ButtonCanvas>(true).ToList();
            foreach (ButtonCanvas buttonCanvas in _buttonCanvases)
            {
                buttonCanvas.OnClickButton += HandleImageChange;
                buttonCanvas.gameObject.SetActive(false);
            }

            _nowButtonType = ButtonType.Serving;
            HandleImageChange(ButtonType.Serving);
        }

        private void HandleImageChange(ButtonType obj)
        {
            ButtonCanvas prevButtonCanvas = _buttonCanvases.FirstOrDefault(x => x.MyButtonType == _nowButtonType);
            if(prevButtonCanvas != null)
                prevButtonCanvas.gameObject.SetActive(false);
            
            ButtonCanvas buttonCanvas = _buttonCanvases.FirstOrDefault<ButtonCanvas>(x => x.MyButtonType == obj);
            if (buttonCanvas != null)
            {
                buttonCanvas.gameObject.SetActive(true);
                _nowButtonType = buttonCanvas.MyButtonType;
                OnClickButton?.Invoke(obj);
            }
        }
    }
}