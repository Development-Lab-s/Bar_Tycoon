using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI
{
    public class CodexManager : MonoBehaviour
    {
        [field: SerializeField] public List<CockTailSlotSo> MyCockTailSlotSos { get; private set; }
        [SerializeField] private EventChannelSO codexChaanelSo;

        private void Awake()
        {
            codexChaanelSo.AddListener<CockTailAddEvent>(HandleCockTailAdd);
        }

        private void HandleCockTailAdd(CockTailAddEvent obj)
        {
            MyCockTailSlotSos.Add(obj.cockTailSlotSo);
        }
    }
}