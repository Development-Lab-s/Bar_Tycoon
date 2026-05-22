using BBJ.WorkplaceSystem;
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [CreateAssetMenu(fileName = "ObjectIconData", menuName = "GridSystem/ObjectIconData")]
    public class ObjectDataSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private TycoonObject workplacePrefab;
        
        public string Id => _id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => _icon;
        public TycoonObject WorkplacePrefab => workplacePrefab;


#if UNITY_EDITOR
        private void OnValidate()
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (_id != guid)
            {
                _id = guid;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
