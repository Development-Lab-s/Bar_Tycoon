using _00._Work.Lusaload._02._Scripts.SO;
using UnityEditor;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.Editor
{
    [CustomEditor(typeof(CocktailRecipeSO))]
    public class CocktailRecipeSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("맛 프로필", EditorStyles.boldLabel);

            CocktailRecipeSO recipe = (CocktailRecipeSO)target;
            DrawTasteBar("쓴맛", recipe.bitterness, new Color(0.35f, 0.15f, 0.05f));
            DrawTasteBar("단맛", recipe.sweetness,  new Color(1f,    0.65f, 0.1f));
            DrawTasteBar("신맛", recipe.sourness,   new Color(0.55f, 0.85f, 0.1f));

            EditorGUILayout.Space(10f);

            if (GUILayout.Button("이 레시피 수정 창 열기", GUILayout.Height(30f)))
            {
                IngredientDatabaseSO db = FindIngredientDatabase();
                CocktailRecipeCreatorWindow.OpenEditWindow((CocktailRecipeSO)target, db, null);
                GUIUtility.ExitGUI();
            }
        }

        private static IngredientDatabaseSO FindIngredientDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:IngredientDatabaseSO");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<IngredientDatabaseSO>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void DrawTasteBar(string label, int value, Color fillColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(40f));

            Rect rect = GUILayoutUtility.GetRect(0f, 16f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));
            if (value > 0)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * (value / 5f), rect.height), fillColor);

            EditorGUILayout.LabelField($"{value} / 5", GUILayout.Width(38f));
            EditorGUILayout.EndHorizontal();
        }
    }
}
