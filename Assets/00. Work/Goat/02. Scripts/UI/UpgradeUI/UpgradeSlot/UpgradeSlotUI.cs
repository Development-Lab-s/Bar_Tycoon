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
        
        private UpgradeData _data;
        public event Action<UpgradeData> OnClickUpgrade;

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
            if (_data == null)
            {
                Debug.Log("no data");
                return;
            }
                
            OnClickUpgrade?.Invoke(_data);
        }
        
        public void SetView(UpgradeData data, string cost)
        {
            _data = data;
            UpgradeDataSO dataSo = _data.upgradeDataSo;
            titleText.text = dataSo.Title;
            descriptionText.text = dataSo.Description;
            costText.text = cost;
        }
    }
}