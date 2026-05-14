using _00._Work.Lusaload._02._Scripts.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI.AlcoholList
{
    public class BaseAlcoholButtonUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image alcoholImage;

        public BaseAlcoholDataSO Data { get; private set; }

        public void Reset()
        {
            nameText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void SetData(BaseAlcoholDataSO dataSO)
        {
            Data = dataSO;

            nameText.text = Data.alcoholName;
        }
    }
}