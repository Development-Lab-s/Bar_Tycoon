using _00._Work.Lusaload._02._Scripts.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI.AlcoholList
{
    // 재료 이름과 아이콘을 표시하며 드래그 소스로 사용되는 단일 재료 버튼 UI
    public class BaseAlcoholButtonUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;  // 재료 이름을 표시할 텍스트
        [SerializeField] private Image alcoholImage;        // 재료 아이콘 이미지
        [SerializeField] private GameObject checkmark;      // 재료 투입 완료 시 표시할 체크 오브젝트

        public BaseAlcoholDataSO Data { get; private set; } // 이 버튼이 나타내는 재료 데이터

        // 에디터에서 Reset 시 자식에서 TextMeshProUGUI를 자동으로 찾아 연결
        public void Reset()
        {
            nameText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // SO 데이터를 받아 이름·이미지를 즉시 갱신
        public void SetData(BaseAlcoholDataSO dataSO)
        {
            Data = dataSO;

            nameText.text = Data.alcoholName;
            alcoholImage.sprite = Data.alcoholImage;
        }

        public void SetChecked(bool isChecked)
        {
            if (checkmark != null)
                checkmark.SetActive(isChecked);
        }
    }
}