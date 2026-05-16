using _00._Work.Lusaload._02._Scripts.SO;
using UnityEditor;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.Editor
{
    [CustomEditor(typeof(AlcoholListSO))]
    public class AlcoholListSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10f);

            if (GUILayout.Button("레시피 생성 창 열기", GUILayout.Height(30f)))
            {
                IngredientDatabaseSO db = FindIngredientDatabase();
                CocktailRecipeCreatorWindow.OpenCreateWindow(db, null);
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
    }
}
