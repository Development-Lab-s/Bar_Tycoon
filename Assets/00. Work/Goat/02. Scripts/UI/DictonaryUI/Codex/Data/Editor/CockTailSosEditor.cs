using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data.Editor
{
    [CustomEditor(typeof(CockTailSlotSos))]
    public class CockTailSosEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset visualTree;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            visualTree.CloneTree(root);
            
            Button btnClick = root.Q<Button>("BtnClick");
            btnClick.clicked += HandleClickButton;
            
            return root;
        }

        private void HandleClickButton()
        {
            CockTailSlotSos cockTailSlotSos = target as CockTailSlotSos;

            if (cockTailSlotSos == null)
            {
                Debug.LogError("cockTailSos is null");
                return;
            }

            int index = 0;
            foreach (CockTailSlotSo cockTailSlotSo in cockTailSlotSos.cockTailSlotList)
            {
                cockTailSlotSo.ChangeId(index);
                index++;
            }
        }
    }
}