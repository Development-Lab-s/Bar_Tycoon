using TMPro;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Coin
{
    public class CoinUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI coinText;

        public void ChangeText(long coin)
        {
            coinText.text = CoinFormatter.Format(coin);
        }
    }
}
