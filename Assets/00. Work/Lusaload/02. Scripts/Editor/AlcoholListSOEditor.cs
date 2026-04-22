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
                Debug.Log("AlcoholListSO 인스펙터 버튼 클릭");
                CocktailRecipeCreatorWindow.OpenCreateWindow((AlcoholListSO)target, null);
                GUIUtility.ExitGUI();
            }
        }
    }
}
