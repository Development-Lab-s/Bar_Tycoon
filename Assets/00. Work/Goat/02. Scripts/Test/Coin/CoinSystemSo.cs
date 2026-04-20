using Unity.Mathematics;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.Test.Coin
{
    [CreateAssetMenu(fileName = "coinSystem", menuName = "SO/coin", order = 0)]
    public class CoinSystemSo : ScriptableObject
    {
        [SerializeField] private int coin;

        public int Coin
        {
            get => coin;
            set => coin = Mathf.Clamp(value, 0, int.MaxValue);
        }

        public void PlusCoin(int value)
        {
            Coin += value;
        }
        
        public void MultiplyCoin(int value)
        {
            Coin *= value;
        }

        public void ResetCoin()
        {
            Coin = 0;
        }
    }
}