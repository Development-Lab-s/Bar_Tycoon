using System.Linq;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEditor;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.Editor
{
    // IngredientDatabaseSO 인스펙터에 카테고리별 재료 목록 폴드아웃과 레시피 생성 창 버튼을 추가하는 커스텀 에디터
    [CustomEditor(typeof(IngredientDatabaseSO))]
    public class IngredientDatabaseSOEditor : UnityEditor.Editor
    {
        private bool _showBaseAlcohol = true; // 기주 카테고리 폴드아웃 상태
        private bool _showDrink       = true; // 음료 카테고리 폴드아웃 상태
        private bool _showGarnish     = true; // 가니시 카테고리 폴드아웃 상태

        // 기본 인스펙터 + 카테고리별 폴드아웃 목록 + 전체 재료 수 + 레시피 창 버튼 표시
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10f);

            IngredientDatabaseSO db = (IngredientDatabaseSO)target;

            DrawCategorySection("기주 (Base Alcohol)", db.baseAlcoholList, ref _showBaseAlcohol);
            DrawCategorySection("음료 (Drink)",        db.drinkList,        ref _showDrink);
            DrawCategorySection("가니시 (Garnish)",    db.garnishList,      ref _showGarnish);

            EditorGUILayout.Space(6f);
            int total = db.GetAll().Count();
            EditorGUILayout.HelpBox($"전체 재료: {total}개", MessageType.None);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("레시피 생성 창 열기", GUILayout.Height(30f)))
            {
                CocktailRecipeCreatorWindow.OpenCreateWindow(db, null);
                GUIUtility.ExitGUI();
            }
        }

        // 지정 AlcoholListSO의 재료 목록을 아이콘·이름 형태의 폴드아웃으로 드로우
        private static void DrawCategorySection(string title, AlcoholListSO list, ref bool foldout)
        {
            foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
            if (!foldout) return;

            EditorGUI.indentLevel++;

            if (list == null || list.alcoholList == null || list.alcoholList.Count == 0)
            {
                EditorGUILayout.LabelField("(비어 있음)", EditorStyles.centeredGreyMiniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            foreach (BaseAlcoholDataSO item in list.alcoholList)
            {
                if (item == null) continue;
                EditorGUILayout.BeginHorizontal();

                Texture icon = item.alcoholImage != null
                    ? AssetPreview.GetAssetPreview(item.alcoholImage) ?? AssetPreview.GetMiniThumbnail(item.alcoholImage)
                    : null;
                if (icon != null)
                    GUILayout.Label(icon, GUILayout.Width(20f), GUILayout.Height(20f));

                EditorGUILayout.LabelField(item.alcoholName);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
    }
}
