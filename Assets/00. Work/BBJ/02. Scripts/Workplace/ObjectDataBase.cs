using System;
using System.Collections.Generic;
using System.Linq;
using _00._Work.Goat._02._Scripts;
using _00._Work.Goat._02._Scripts.SaveCode;
using UnityEngine;
using BBJ.GridSystem.Objects;

namespace BBJ.WorkplaceSystem
{
    // ���� �� ������ ������ ���� �����ͺ��̽� ScriptableObject
    [CreateAssetMenu(fileName = "ObjectDataBase", menuName = "GridSystem/ObjectDataBase", order = 0)]
    public class ObjectDataBase : ScriptableObject, ISerializationCallbackReceiver
    {
        [Header("Save")]
        [SerializeField] private SaveFileNameSO saveFileNameSO;
        
        [Header("All Object Data")]
        [SerializeField] private List<ObjectDataSO> allObjects = new();
        
        [Header("First Item")]
        [SerializeField] private List<ObjectDataSO> firstItem;
        
        [SerializeField] private List<ObjectDataSO> itemListForSerialize = new();
        public IReadOnlyCollection<ObjectDataSO> Recipes => recipes;

        private readonly HashSet<ObjectDataSO> recipes = new();
        private readonly Dictionary<string, ObjectDataSO> idCache = new();
        
        private JsonSaveService _saveService;


        private void OnEnable()
        {
            if (saveFileNameSO != null)
                _saveService = new JsonSaveService(saveFileNameSO);
            
            LoadSerializedListToHashSet();
            Load();

        }

        public void AddCockTail(ObjectDataSO objectDataSO)
        {
            if (objectDataSO == null || objectDataSO.Id == null) return;

            if (recipes.Add(objectDataSO))
            {
                idCache[objectDataSO.Id] = objectDataSO;
                Save();
            }
        }
        
        public void Save()
        {
            if (_saveService == null)
            {
                if (saveFileNameSO == null)
                {
                    Debug.LogWarning("SaveFileNameSO가 없습니다.");
                    return;
                }

                _saveService = new JsonSaveService(saveFileNameSO);
            }

            ObjectDataBaseSaveData saveData = new ObjectDataBaseSaveData();

            foreach (var item in recipes)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;

                saveData.ids.Add(item.Id);
            }

            _saveService.Save(saveData);
        }
        public void Load()
        {
            if (_saveService == null)
            {
                if (saveFileNameSO == null)
                    return;

                _saveService = new JsonSaveService(saveFileNameSO);
            }

            ObjectDataBaseSaveData saveData =
                _saveService.Load<ObjectDataBaseSaveData>();

            // 저장 데이터가 없으면 처음 아이템들 지급
            if (saveData == null)
            {
                Reset();

                AddFirstItems();

                Save();
                return;
            }

            recipes.Clear();
            idCache.Clear();

            foreach (string id in saveData.ids)
            {
                ObjectDataSO item = allObjects
                    .FirstOrDefault(x => x != null && x.Id == id);

                if (item == null)
                    continue;

                recipes.Add(item);
                idCache[item.Id] = item;
            }

            // 저장 파일은 있는데 안에 아무것도 없으면 처음 아이템들 지급
            if (recipes.Count == 0)
            {
                AddFirstItems();
                Save();
            }
        }
        private void AddFirstItems()
        {
            foreach (var item in firstItem)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;

                bool isAdded = recipes.Add(item);

                if (isAdded)
                    idCache[item.Id] = item;
            }
        }


        // ����ȭ �� HashSet �����͸� List�� ��ȯ
        public void OnBeforeSerialize()
        {
            itemListForSerialize.Clear();

            // null ����
            recipes.Remove(null);
            // ���� ����ȭ�� ����Ʈ�� ���� ���� ä����
            itemListForSerialize.AddRange(recipes);
        }

        // ������ȭ �� List �����͸� HashSet�� ����
        public void OnAfterDeserialize()
        {
            LoadSerializedListToHashSet();
        }
        private void LoadSerializedListToHashSet()
        {
            recipes.Clear();
            idCache.Clear();

            recipes.UnionWith(itemListForSerialize);
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