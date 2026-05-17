using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Core.Sorting.Editor
{
    [CustomEditor(typeof(DepthSortingByPoint)), CanEditMultipleObjects]
    public class DepthSortingByPointEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var t = (DepthSortingByPoint)target;
            EditorGUILayout.Space(4);

            if (GUILayout.Button("Apply Now"))
            {
                Undo.RecordObjects(targets, "Apply Depth Sort");
                foreach (var obj in targets)
                    ((DepthSortingByPoint)obj).Apply();
            }

            // Read-only display of the currently computed order
            EditorGUILayout.Space(2);
            GUI.enabled = false;
            EditorGUILayout.IntField("Computed Order", t.ComputeOrder());
            GUI.enabled = true;
        }

        void OnSceneGUI()
        {
            var t = (DepthSortingByPoint)target;
            if (t == null || t.SortPoint == null) return;

            GUIStyle style = new GUIStyle
            {
                normal = { textColor = new Color(1f, 0.85f, 0f) },
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            Handles.Label(t.SortPoint.position + Vector3.up * 0.12f, $"sort: {t.ComputeOrder()}", style);
        }
    }
}
