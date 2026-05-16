using _00._Work.Lusaload._02._Scripts.SO;
using UnityEditor;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.Editor
{
    // AlcoholListSO 인스펙터에 레시피 생성 창 단축 버튼을 추가하는 커스텀 에디터
    [CustomEditor(typeof(AlcoholListSO))]
    public class AlcoholListSOEditor : UnityEditor.Editor
    {
        // 기본 인스펙터 드로우 후 레시피 생성 창을 여는 버튼 표시
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

        // 프로젝트 내 첫 번째 IngredientDatabaseSO를 자동으로 찾아 반환
        private static IngredientDatabaseSO FindIngredientDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:IngredientDatabaseSO");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<IngredientDatabaseSO>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
