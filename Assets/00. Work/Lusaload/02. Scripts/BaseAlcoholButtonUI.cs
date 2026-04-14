using _00._Work.Lusaload._02._Scripts.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts
{
    public class BaseAlcoholButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image alcoholImage;

        private BaseAlcoholDataSO _data;

        public void Reset()
        {
            button = GetComponent<Button>();
            nameText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void SetData(BaseAlcoholDataSO data)
        {
            _data = data;

            nameText.text = data.alcoholName;
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickButton);
        }

        private void OnClickButton()
        {
            Debug.Log($"선택한 술 : {_data.alcoholName}");
        }
    }
}