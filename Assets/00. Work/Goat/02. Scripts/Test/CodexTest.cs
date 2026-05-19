using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Lusaload._02._Scripts.SO;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Test
{
    public class CodexTest : MonoBehaviour
    {
        [SerializeField] private CocktailRecipeDatabaseSO dataBase;
        [SerializeField] private CocktailRecipeSO cockTail;

        [ContextMenu("Ez")]
        public void Ez()
        {
            dataBase.AddCockTail(cockTail);
        }
    }
}