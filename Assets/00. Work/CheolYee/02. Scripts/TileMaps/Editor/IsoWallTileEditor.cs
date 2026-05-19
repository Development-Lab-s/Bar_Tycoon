using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using _00._Work.CheolYee._02._Scripts.TileMaps;

namespace _00._Work.CheolYee._02._Scripts.TileMaps.Editor
{
    [CustomEditor(typeof(IsoWallTile))]
    public class IsoWallTileEditor : UnityEditor.Editor
    {
        private const string k_CenterTileProp = "centerTilePosition";

        private bool _isPicking;
        private Tilemap _referenceTilemap;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("─ Center Tile Picker ─", EditorStyles.boldLabel);

            _referenceTilemap = (Tilemap)EditorGUILayout.ObjectField(
                "Reference Tilemap", _referenceTilemap, typeof(Tilemap), allowSceneObjects: true);

            using (new EditorGUI.DisabledScope(_referenceTilemap == null))
            {
                string label = _isPicking ? "[ 씬 클릭으로 타일 선택 중... 취소 ]" : "Pick Center Tile from Scene";
                if (GUILayout.Button(label))
                    _isPicking = !_isPicking;
            }

            if (_isPicking)
                EditorGUILayout.HelpBox("씬 뷰에서 타일을 클릭하면 Center Tile Position이 설정됩니다.", MessageType.Info);

            if (_referenceTilemap == null)
                EditorGUILayout.HelpBox("Reference Tilemap을 씬에서 드래그해서 연결하세요.", MessageType.Warning);
        }

        private void OnSceneGUI()
        {
            if (!_isPicking || _referenceTilemap == null) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0) return;

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            // Intersect with Z=0 plane (standard isometric view)
            if (Mathf.Abs(ray.direction.z) < 0.0001f) return;

            float t = -ray.origin.z / ray.direction.z;
            Vector3 worldPos = ray.origin + ray.direction * t;
            Vector3Int cellPos = _referenceTilemap.WorldToCell(worldPos);

            serializedObject.Update();
            serializedObject.FindProperty(k_CenterTileProp).vector3IntValue = cellPos;
            serializedObject.ApplyModifiedProperties();

            _isPicking = false;
            e.Use();

            _referenceTilemap.RefreshTile(cellPos);
        }

        private void OnDisable()
        {
            _isPicking = false;
        }
    }
}
