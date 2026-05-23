using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BBJ.GridSystem.Objects;

namespace BBJ.WorkplaceSystem
{
    // 도감 및 레시피 관리를 위한 데이터베이스 ScriptableObject
    [CreateAssetMenu(fileName = "ObjectDataBase", menuName = "GridSystem/ObjectDataBase", order = 0)]
    public class ObjectDataBase : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<ObjectDataSO> itemListForSerialize = new();
        public IReadOnlyCollection<ObjectDataSO> Recipes => recipes;

        private readonly HashSet<ObjectDataSO> recipes = new();
        private readonly Dictionary<string, ObjectDataSO> idCache = new();

        public void AddCockTail(ObjectDataSO objectDataSO)
        {
            if (objectDataSO == null || objectDataSO.Id == null) return;

            if (recipes.Add(objectDataSO))
                idCache[objectDataSO.Id] = objectDataSO;
        }

        // 직렬화 전 HashSet 데이터를 List로 변환
        public void OnBeforeSerialize()
        {
            itemListForSerialize.Clear();

            // null 제거
            recipes.Remove(null);
            // 기존 직렬화용 리스트를 비우고 새로 채워줌
            itemListForSerialize.AddRange(recipes);
        }

        // 역직렬화 후 List 데이터를 HashSet에 삽입
        public void OnAfterDeserialize()
        {
            recipes.Clear();
            idCache.Clear();

            // .ToHashSet()은 매번 새로운 메모리 공간을 할당하기 때문에 메모리 파편화와 가비지를 유발
            // 중복 없도록 합집합(union)으로 만들기
            recipes.UnionWith(itemListForSerialize);

            // null 제거
            recipes.Remove(null);

            foreach (var item in recipes)
            {
                if (item != null && !string.IsNullOrEmpty(item.Id))
                    idCache[item.Id] = item;
            }
        }

        public void Reset()
        {
            recipes.Clear();
            idCache.Clear();
            itemListForSerialize.Clear();
        }
        public ObjectDataSO GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return default;

            if (!idCache.TryGetValue(id, out var result))
                result = default;

            return result;
        }
    }
}