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
        [SerializeField] private TextMeshProUGUI nowLevel;
        [SerializeField] private TextMeshProUGUI nextLevel;
        [SerializeField] private TextMeshProUGUI nowStat;
        [SerializeField] private TextMeshProUGUI nextStat;
        
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
        
        public void SetView(UpgradeData data)
        {
            _data = data;
            UpgradeDataSO dataSo = _data.UpgradeDataSo;
            titleText.text = dataSo.Title;
            descriptionText.text = dataSo.Description;
            costText.text = data.GetCost();
            nowLevel.text = data.GetLevel(0);
            nextLevel.text = data.GetLevel(1);
            nowStat.text = (data.UpgradeDataSo.TargetStat.BaseValue + data.GetTotalIncreaseValue() ).ToString();
            nextStat.text = (data.UpgradeDataSo.TargetStat.BaseValue + data.GetTotalIncreaseValue() + data.UpgradeDataSo.IncreaseValue).ToString();
        }
    }
}