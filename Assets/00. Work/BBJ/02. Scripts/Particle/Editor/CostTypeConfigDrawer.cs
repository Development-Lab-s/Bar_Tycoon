using UnityEditor;
using UnityEngine;

namespace BBJ.Particle.Editor
{
    [CustomPropertyDrawer(typeof(CostTypeConfig))]
    public class CostTypeConfigDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float pad   = EditorGUIUtility.standardVerticalSpacing;

            if (!property.isExpanded) return lineH;

            float h = lineH + pad;
            SerializedProperty it  = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (SerializedProperty.EqualContents(it, end)) break;
                h += EditorGUI.GetPropertyHeight(it, true) + pad;
            }
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineH = EditorGUIUtility.singleLineHeight;
            float pad   = EditorGUIUtility.standardVerticalSpacing;
            float y     = position.y;

            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, y, position.width, lineH),
                property.isExpanded, label, true);
            y += lineH + pad;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                SerializedProperty it  = property.Copy();
                SerializedProperty end = property.GetEndProperty();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (SerializedProperty.EqualContents(it, end)) break;
                    float fieldH = EditorGUI.GetPropertyHeight(it, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, fieldH), it, true);
                    y += fieldH + pad;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
