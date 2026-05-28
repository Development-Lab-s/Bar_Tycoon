using System;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Coin
{
    public class CoinCanvas : MonoBehaviour
    {
        [SerializeField] private CoinUI     coinUI;
        [SerializeField] private CoinManager coinManager;

        private void Awake()
        {
            coinManager.OnChangeCoin += HandleChangeCoin;
        }

        private void Start()
        {
            HandleChangeCoin(coinManager.CurrentCoin);
        }

        private void OnDestroy()
        {
            coinManager.OnChangeCoin -= HandleChangeCoin;
        }

        private void HandleChangeCoin(long coin)
        {
            coinUI.ChangeText(coin);
        }
    }
}
