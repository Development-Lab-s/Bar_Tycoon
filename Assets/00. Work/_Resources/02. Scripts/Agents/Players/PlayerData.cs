using System;
using _00._Work._Resources._02._Scripts.Modules;
using _00._Work._Resources._02._Scripts.Systems.CoreSystem;
using _00._Work._Resources._02._Scripts.Systems.GameEvents;
using Gamelib.EventSystem;
using UnityEngine;

namespace _00._Work._Resources._02._Scripts.Agents.Players
{
    public class PlayerData : MonoBehaviour, IModule, ISaveable
    {
        [field: SerializeField] public SaveIdData SaveId { get; private set; }
        [field: SerializeField] public int Gold { get; private set; }
        [field: SerializeField] public int Gem { get; private set; }
        [field: SerializeField] public EventChannelSO PlayerEventChannel { get; private set; }

        public event Action<int> OnGoldChanged;
        public event Action<int> OnGemChanged;
        
        private ModuleOwner _owner;

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            Debug.Assert(_owner != null, $"{gameObject.name} 모듈화 시스템 하위 족속에 붙어있지 않습니다.");
        }
        
        private void Start()
        {
            PlayerEventChannel?.RaiseEvent(PlayerEvents.PlayerDataSetUpEvent.Init(this));
        }
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || Gold < amount)
                return false;

            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }

        public void AddGem(int amount)
        {
            if (amount <= 0) return;
            Gem += amount;
            OnGemChanged?.Invoke(Gem);
        }

        public bool TrySpendGem(int amount)
        {
            if (amount <= 0 || Gem < amount)
                return false;

            Gem -= amount;
            OnGemChanged?.Invoke(Gem);
            return true;
        }

        public void SetGold(int amount)
        {
            Gold = Mathf.Max(0, amount);
            OnGoldChanged?.Invoke(Gold);
        }

        public void SetGem(int amount)
        {
            Gem = Mathf.Max(0, amount);
            OnGemChanged?.Invoke(Gem);
        }

        [Serializable]
        private struct PlayerSaveData
        {
            public int gold;
            public int gem;
        }

        public string GetSaveData()
        {
            PlayerSaveData saveData = new PlayerSaveData
            {
                gold = Gold,
                gem = Gem
            };

            return JsonUtility.ToJson(saveData, true);
        }

        public void RestoreData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            PlayerSaveData saveData = JsonUtility.FromJson<PlayerSaveData>(data);
            Gold = Mathf.Max(0, saveData.gold);
            Gem = Mathf.Max(0, saveData.gem);

            OnGoldChanged?.Invoke(Gold);
            OnGemChanged?.Invoke(Gem);
        }
    }
}