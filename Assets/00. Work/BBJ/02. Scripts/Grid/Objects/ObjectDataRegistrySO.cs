using System.Collections.Generic;
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [CreateAssetMenu(fileName = "ObjectDataRegistry", menuName = "GridSystem/ObjectDataRegistry")]
    public class ObjectDataRegistrySO : ScriptableObject
    {
        [SerializeField] private List<ObjectDataSO> _objects = new();

        private Dictionary<string, ObjectDataSO> _dict;

        public void BuildRuntimeDict()
        {
            _dict = new Dictionary<string, ObjectDataSO>(_objects.Count);
            foreach (var obj in _objects)
            {
                if (obj != null && !string.IsNullOrEmpty(obj.Id))
                    _dict[obj.Id] = obj;
            }
        }

        public ObjectDataSO GetById(string id)
        {
            if (_dict == null) BuildRuntimeDict();
            _dict.TryGetValue(id, out var result);
            return result;
        }

#if UNITY_EDITOR
        [ContextMenu("Scan Project")]
        private void ScanProject()
        {
            _objects.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:ObjectDataSO");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var obj  = UnityEditor.AssetDatabase.LoadAssetAtPath<ObjectDataSO>(path);
                if (obj != null)
                    _objects.Add(obj);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ObjectDataRegistry] Scanned {_objects.Count} assets.");
        }
#endif
    }
}
