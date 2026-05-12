using BBJ.Data;
using BBJ.Work;
using UnityEngine;
using _00._Work._Resources._02._Scripts.Modules;

namespace BBJ.Modules
{
    public class FoodContextModule : MonoBehaviour, IModule, ICurrentFoodProvider
    {
        public FoodDataSO CurrentFood { get; private set; }

        public void Initialize(ModuleOwner owner) { }

        public void SetFood(FoodDataSO food) => CurrentFood = food;
        public void ClearFood()              => CurrentFood = null;
    }
}
