using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.UI.AlcoholList
{
    public class AlcoholScrollListUI : MonoBehaviour
    {
        [SerializeField] private AlcoholListSO alcoholListSO;

        [SerializeField] private Transform contentParent;
        [SerializeField] private BaseAlcoholButtonUI buttonPrefab;

        private void Awake()
        {
            Debug.Assert(alcoholListSO != null, "alcoholListSO가 비어있습니다.");
        }

        private void Start()
        {
            CreateButton();
        }

        private void CreateButton()
        {
            ClearChildren();
            
            foreach (BaseAlcoholDataSO alcoholData in alcoholListSO.alcoholList)
            {
                if(alcoholData == null) continue;

                BaseAlcoholButtonUI buttonUI = Instantiate(buttonPrefab, contentParent);
                buttonUI.SetData(alcoholData);
            }
            
        }

        private void ClearChildren()
        {
            for (int i = 0; i < contentParent.childCount; i++)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }
    }
}