using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using _00._Work.Lusaload._02._Scripts.SO;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Test
{
    public class CodexTest : MonoBehaviour
    {
        [SerializeField] private EventChannelSO codexChaanelSo;
        [SerializeField] private CocktailRecipeSO cockTail;

        [ContextMenu("Ez")]
        public void Ez()
        {
            codexChaanelSo.RaiseEvent(new CockTailAddEvent().Init(cockTail));
        }
    }
}