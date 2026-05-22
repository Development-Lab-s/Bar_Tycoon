using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BBJ.GridSystem.Objects
{
    [CreateAssetMenu(fileName = "ObjectBatch", menuName = "GridSystem/ObjectBatch")]
    public class ObjectsBatchSO : ScriptableObject
    {
        [SerializeField] private StageLayoutSO objectsLayout;
        [SerializeField] private int stage;
        [SerializeField, HideInInspector] private string _id;

        // 가구에 위치 및 생성에 필요한 데이터 목록
        public List<PlacedObstacleEntry> ObjectsLayout => objectsLayout.entries;

        // 도감같은 곳에서 필요할 데이터 목록
        public HashSet<ObjectDataSO> ObjectsSet
        {
            get
            {
                if (_objectsSet == null) InitializeObjectsSet();
                return _objectsSet;
            }
        }
        public int Stage => stage;
        public string Id => _id;

        private HashSet<ObjectDataSO> _objectsSet;

        private void InitializeObjectsSet()
        {
            // 기존 인스턴스가 없다면 최초 1회 생성
            if (_objectsSet == null) _objectsSet = new HashSet<ObjectDataSO>();
            else _objectsSet.Clear();

            if (objectsLayout == null || objectsLayout.entries == null) return;

            // .ToHashSet()은 매번 새로운 메모리 공간을 할당하기 때문에 메모리 파편화와 가비지를 유발
            // 작성하신 LINQ 스타일을 유지하되, 기존 주머니(_objectsSet)에 합집합으로 밀어넣어 가비지 방지
            _objectsSet.UnionWith(objectsLayout.entries.ConvertAll(e => e.obstacleData));
            _objectsSet.Remove(null);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InitializeObjectsSet();

            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

            if (_id != guid && !string.IsNullOrEmpty(guid))
            {
                _id = guid;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}