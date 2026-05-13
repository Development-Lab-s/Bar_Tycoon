using System;
using _00._Work.Goat._02._Scripts.Coin.CoinDatas;
using _00._Work.Goat._02._Scripts.Events;
using _00._Work.Goat._02._Scripts.SaveCode;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Coin
{
    public class CoinManager : MonoBehaviour
    {
        [SerializeField] private EventChannelSO coinChannelSO;
        [SerializeField] private SaveFileNameSO saveFileNameSO;
        
        private CoinData _coinData;
        private JsonSaveService _jsonSaveService;
        
        public event Action<int> OnChangeCoin;

        private void Awake()
        {
            _jsonSaveService = new JsonSaveService(saveFileNameSO);
            LoadCoin();
            
            coinChannelSO.AddListener<CoinEvent>(HandleCoinEvent);
        }

        private void OnDestroy()
        {
            coinChannelSO.RemoveListener<CoinEvent>(HandleCoinEvent);
        }

        private void LoadCoin()
        {
            _coinData = _jsonSaveService.Load<CoinData>();
            
            if (_coinData == null)
            {
                _coinData = new CoinData();
                _coinData.coin = 0;
            }
            
            OnChangeCoin?.Invoke(_coinData.coin);
        }

        private void HandleCoinEvent(CoinEvent coin)
        {
            _coinData.coin += coin.amount;
            OnChangeCoin?.Invoke(_coinData.coin);
            _jsonSaveService.Save(_coinData);
        }
    }
}