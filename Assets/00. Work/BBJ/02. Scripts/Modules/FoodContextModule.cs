using BBJ.Work;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work.Lusaload._02._Scripts.SO;

namespace BBJ.Modules
{
    public class FoodContextModule : MonoBehaviour, IModule, ICurrentFoodProvider
    {
        public CocktailRecipeSO CurrentFood { get; private set; }

        public void Initialize(ModuleOwner owner) { }

        public void SetFood(CocktailRecipeSO food) => CurrentFood = food;
        public void ClearFood()                    => CurrentFood = null;
    }
}
