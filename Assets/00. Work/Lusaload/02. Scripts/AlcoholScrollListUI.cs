using System;
using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts
{
    public class AlcoholScrollListUI : MonoBehaviour
    {
        [SerializeField] private AlcoholListSO alcoholListSO;

        [SerializeField] private Transform contentParent;
        [SerializeField] private BaseAlcoholButtonUI buttonPrefab;

        private void Awake()
        {
            Debug.Assert(alcoholListSO != null, "Alcohol List 데이터가 존재하지 않습니다.");
        }

        private void Start()
        {
            CreateButton();
        }

        private void CreateButton()
        {
            foreach (BaseAlcoholDataSO alcoholData in alcoholListSO.alcoholList)
            {
                if(alcoholData == null) continue;

                BaseAlcoholButtonUI buttonUI = Instantiate(buttonPrefab, contentParent);
                buttonUI.SetData(alcoholData);
            }
        }

        public void ClearChildren()
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }
    }
}