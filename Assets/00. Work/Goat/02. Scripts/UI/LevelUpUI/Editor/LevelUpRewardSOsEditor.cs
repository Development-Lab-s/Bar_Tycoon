using _00._Work.Lusaload._02._Scripts.SO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI.Editor
{
    [CustomEditor(typeof(LevelUpRewardSOs))]
    public class LevelUpRewardSOsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            
            Button renameButton = new Button(RenameRewards)
            {
                text = "배열 순서대로 SO 이름 변경" 
            };
            
            root.Add(renameButton);
            
            return root;
        }
        
        private void RenameRewards()
        {
            LevelUpRewardSOs data = (LevelUpRewardSOs)target;

            for (int i = 0; i < data.levelUpRewardSOs.Length; i++)
            {
                LevelUpRewardSO reward = data.levelUpRewardSOs[i];

                if (reward == null)
                    continue;
                
                int unlockStage = i + 2;

                string path = AssetDatabase.GetAssetPath(reward);
                if (string.IsNullOrEmpty(path))
                    continue;

                AssetDatabase.RenameAsset(path, $"{i + 1}_LevelReward");
                EditorUtility.SetDirty(reward);
                
                if (reward.cockTails == null)
                    continue;

                foreach (CocktailRecipeSO cocktail in reward.cockTails)
                {
                    if (cocktail == null)
                        continue;

                    cocktail.unlockStage = unlockStage / 2;
                    EditorUtility.SetDirty(cocktail);
                }
            }

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}