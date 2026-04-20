using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.BtnCanvas;
using _00._Work.Goat._02._Scripts.UI.UpgradeUI.UpgradeSlot;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.UpgradeUI
{
    [Serializable]
    public class ButtonUpgradeData
    {
        public ButtonType buttonType;
        public List<UpgradeData> upgradeGroups;
        public EventChannelSO upgradeChannel;
    }
}