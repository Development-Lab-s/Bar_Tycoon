using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot
{
    public class UpgradeSlotUI : MonoBehaviour
    {
        [Header("UIs")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button upgradeButton;
        
        public event Action OnClickUpgrade;

        private void Awake()
        {
            upgradeButton.onClick.AddListener(HandleClickUpgrade);
        }

        private void OnDestroy()
        {
            upgradeButton.onClick.RemoveListener(HandleClickUpgrade);
        }

        private void HandleClickUpgrade()
        {
            OnClickUpgrade?.Invoke();
        }

        #region SetUI
        
        public void SetView(string title, string description, string cost)
        {
            titleText.text = title;
            descriptionText.text = description;
            costText.text = cost;
        }
        
        public void SetTitle()
        {
            titleText.text = titleText.text;
        }

        public void SetDescription()
        {
            descriptionText.text = descriptionText.text;
        }

        public void SetCost()
        {
            costText.text = costText.text;
        }
        #endregion
    }
}