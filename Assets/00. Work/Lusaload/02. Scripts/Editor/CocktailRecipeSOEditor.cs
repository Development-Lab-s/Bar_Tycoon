using _00._Work.Lusaload._02._Scripts.SO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.Lusaload._02._Scripts.Editor
{
    [CustomEditor(typeof(CocktailRecipeSO))]
    public class CocktailRecipeSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10f);

            if (GUILayout.Button("이 레시피 수정 창 열기", GUILayout.Height(30f)))
            {
                Debug.Log("CocktailRecipeSO 인스펙터 버튼 클릭");
                CocktailRecipeCreatorWindow.OpenEditWindow((CocktailRecipeSO)target, null, null);
                GUIUtility.ExitGUI();
            }
        }
    }
}