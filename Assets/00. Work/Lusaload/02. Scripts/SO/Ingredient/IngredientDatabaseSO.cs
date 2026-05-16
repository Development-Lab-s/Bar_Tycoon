using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.SO
{
    // 기주·음료·가니시 리스트를 한 곳에서 관리하고 카테고리별 조회를 제공하는 데이터베이스 SO
    [CreateAssetMenu(fileName = "IngredientDatabase", menuName = "Alcohol/IngredientDatabase", order = 3)]
    public class IngredientDatabaseSO : ScriptableObject
    {
        public AlcoholListSO baseAlcoholList; // 기주(BaseAlcohol) 목록 SO
        public AlcoholListSO drinkList;       // 음료(Drink) 목록 SO
        public AlcoholListSO garnishList;     // 가니시(Garnish) 목록 SO

        // 리스트가 null일 때 빈 컬렉션을 반환해 NullReference를 방지하는 공유 인스턴스
        private static readonly List<BaseAlcoholDataSO> _empty = new();

        // 카테고리 열거값으로 해당 재료 리스트를 반환. 미연결이면 빈 리스트 반환
        public List<BaseAlcoholDataSO> GetList(IngredientCategory category) => category switch
        {
            IngredientCategory.BaseAlcohol => baseAlcoholList?.alcoholList ?? _empty,
            IngredientCategory.Drink       => drinkList?.alcoholList       ?? _empty,
            IngredientCategory.Garnish     => garnishList?.alcoholList     ?? _empty,
            _                              => _empty
        };

        // 세 카테고리 전체를 순서대로 이어 붙인 시퀀스를 반환
        public IEnumerable<BaseAlcoholDataSO> GetAll() =>
            GetList(IngredientCategory.BaseAlcohol)
                .Concat(GetList(IngredientCategory.Drink))
                .Concat(GetList(IngredientCategory.Garnish));
    }
}
