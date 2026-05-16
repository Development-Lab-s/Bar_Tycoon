using _00._Work.Lusaload._02._Scripts.SO;
using UnityEngine;

namespace _00._Work.Lusaload._02._Scripts.UI.AlcoholList
{
    // 고정된 AlcoholListSO를 기반으로 재료 버튼 목록을 생성하는 스크롤 리스트 UI
    public class AlcoholScrollListUI : MonoBehaviour
    {
        [SerializeField] private AlcoholListSO alcoholListSO;   // 표시할 재료 목록 SO
        [SerializeField] private Transform contentParent;        // 버튼들이 생성될 부모 Transform
        [SerializeField] private BaseAlcoholButtonUI buttonPrefab; // 재료 버튼 프리팹

        private void Awake()
        {
            Debug.Assert(alcoholListSO != null, "alcoholListSO가 비어있습니다.");
        }

        private void Start()
        {
            CreateButton();
        }

        // alcoholListSO의 모든 재료에 대해 버튼을 생성
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

        // contentParent의 모든 자식 오브젝트를 제거
        private void ClearChildren()
        {
            for (int i = 0; i < contentParent.childCount; i++)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }
    }
}