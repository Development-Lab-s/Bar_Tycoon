using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    [CreateAssetMenu(fileName = "IngredientDatabase", menuName = "Alcohol/IngredientDatabase", order = 3)]
    public class IngredientDatabaseSO : ScriptableObject
    {
        public AlcoholListSO baseAlcoholList;
        public AlcoholListSO drinkList;
        public AlcoholListSO garnishList;

        private static readonly List<BaseAlcoholDataSO> _empty = new();

        public List<BaseAlcoholDataSO> GetList(IngredientCategory category) => category switch
        {
            IngredientCategory.BaseAlcohol => baseAlcoholList?.alcoholList ?? _empty,
            IngredientCategory.Drink       => drinkList?.alcoholList       ?? _empty,
            IngredientCategory.Garnish     => garnishList?.alcoholList     ?? _empty,
            _                              => _empty
        };

        public IEnumerable<BaseAlcoholDataSO> GetAll() =>
            GetList(IngredientCategory.BaseAlcohol)
                .Concat(GetList(IngredientCategory.Drink))
                .Concat(GetList(IngredientCategory.Garnish));
    }
}
