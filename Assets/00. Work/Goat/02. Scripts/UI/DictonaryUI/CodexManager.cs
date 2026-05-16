using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using _00._Work.Lusaload._02._Scripts.SO;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI
{
    public class CodexManager : MonoBehaviour
    {
        [Header("All CockTail Type")]
        [SerializeField] private CocktailRecipeDatabaseSO allCockTailType;
        
        [Header("Sos")]
        [SerializeField] private EventChannelSO codexChaanelSo;
        
        public CocktailRecipeDatabaseSO UnlockedCockTails { get; private set; } = new();

        public event Action<List<CocktailRecipeSO>> OnAddCockTail;

        private void Awake()
        {
            codexChaanelSo.AddListener<CockTailAddEvent>(HandleCockTailAdd);
        }

        private void OnDestroy()
        {
            codexChaanelSo.RemoveListener<CockTailAddEvent>(HandleCockTailAdd);
        }

        private void HandleCockTailAdd(CockTailAddEvent obj)
        {
            UnlockedCockTails.recipes.Add(obj.cockTailSlotSo);
            OnAddCockTail?.Invoke(UnlockedCockTails.recipes);
        }
    }
}