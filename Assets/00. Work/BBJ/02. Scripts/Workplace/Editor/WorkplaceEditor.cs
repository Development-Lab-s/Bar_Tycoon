#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BBJ.WorkplaceSystem.Editor
{
    [CustomEditor(typeof(Workplace))]
    public class WorkplaceEditor : UnityEditor.Editor
    {
        private SerializedProperty _tileSetDataProp;
        private SerializedProperty _interactPointsProp;
        private SerializedProperty _flipXProp;

        private void OnEnable()
        {
            _tileSetDataProp    = serializedObject.FindProperty("_tileSetData");
            _interactPointsProp = serializedObject.FindProperty("_interactPoints");
            _flipXProp          = serializedObject.FindProperty("_flipX");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Workplace 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_register"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_workplaceType"));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("타일 & 인터랙션", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_tileSetDataProp);
            EditorGUILayout.PropertyField(_flipXProp);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_interactPointsProp, new GUIContent("InteractPoints"), true);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Gizmo", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_showGizmos"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_gizmoTileSize"));

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var wp     = (Workplace)target;
            var points = serializedObject.FindProperty("_interactPoints");
            bool flipX = serializedObject.FindProperty("_flipX").boolValue;

            for (int i = 0; i < points.arraySize; i++)
            {
                var element    = points.GetArrayElementAtIndex(i);
                var offsetProp = element.FindPropertyRelative("Offset");
                var roleProp   = element.FindPropertyRelative("Role");

                var roleObj    = roleProp.objectReferenceValue as InteractRoleSO;
                Color gizmoColor = roleObj != null ? roleObj.GizmoColor : Color.white;

                int ox = offsetProp.FindPropertyRelative("x").intValue;
                int oy = offsetProp.FindPropertyRelative("y").intValue;
                if (flipX) ox = -ox;

                Vector3 worldPos = wp.transform.position + new Vector3(ox, 0f, oy);

                Handles.color = gizmoColor;
                Handles.SphereHandleCap(0, worldPos, Quaternion.identity, 0.2f, EventType.Repaint);

                Handles.Label(worldPos + Vector3.up * 0.3f,
                    $"[{i}] {(roleObj != null ? roleObj.name : "No Role")} ({ox},{oy})",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = gizmoColor } });
            }
        }
    }
}
#endif
