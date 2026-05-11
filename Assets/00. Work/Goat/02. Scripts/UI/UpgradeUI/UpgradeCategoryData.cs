using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    [Serializable]
    public class UpgradeCategoryData
    {
        [field: SerializeField] public ButtonType ButtonType { get; private set; }
        [field: SerializeField] public List<UpgradeData> UpgradeGroups { get; private set; }
        [field: SerializeField] public EventChannelSO UpgradeChannel { get; private set; }
    }
}