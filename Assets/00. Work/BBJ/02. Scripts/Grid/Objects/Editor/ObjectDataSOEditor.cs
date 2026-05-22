using UnityEngine;
using UnityEditor;

namespace BBJ.GridSystem.Objects.Editor
{
    [CustomEditor(typeof(ObjectDataSO))]
    public class ObjectDataSOEditor : UnityEditor.Editor
    {
        private const float PreviewSize = 100f;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var iconProp = serializedObject.FindProperty("_icon");
            EditorGUILayout.PropertyField(iconProp);

            if (iconProp.objectReferenceValue is Sprite sprite)
                DrawIconPreview(sprite);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("workplacePrefab"));

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawIconPreview(Sprite sprite)
        {
            EditorGUILayout.Space(4);

            Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));
            rect.x = (EditorGUIUtility.currentViewWidth - PreviewSize) * 0.5f;

            EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.16f, 1f));

            Texture2D tex = sprite.texture;
            Rect uv = new Rect(
                sprite.textureRect.x      / tex.width,
                sprite.textureRect.y      / tex.height,
                sprite.textureRect.width  / tex.width,
                sprite.textureRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv, true);

            EditorGUILayout.Space(4);
        }
    }
}
