using System.Collections.Generic;
using _00._Work.Lusaload._02._Scripts.SO;
using BBJ.GridSystem.Objects;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    public class LevelUpContainer : MonoBehaviour
    {
        [SerializeField] private LevelUpRewardSlot levelUpRewardSlot;

        [Header("detail")] 
        [SerializeField] private int spawnCount;
        
        private List<LevelUpRewardSlot>  levelUpRewardSlots = new();

        private void Awake()
        {
            Init(spawnCount);
        }

        private void OnDisable()
        {
            HideAllSlot();
        }

        public void SpawnSlotCockTail(CocktailRecipeSO levelUpReward)
        {
            LevelUpRewardSlot slot = GetInactiveSlot();

            if (slot == null)
            {
                slot = CreateSlot();
            }

            slot.SetImage(levelUpReward.cocktailIcon);
            slot.gameObject.SetActive(true);
        }

        public void SpawnSlotSprite(Sprite sprite)
        {
            LevelUpRewardSlot slot = GetInactiveSlot();

            if (slot == null)
            {
                slot = CreateSlot();
            }

            slot.SetImage(sprite);
            slot.gameObject.SetActive(true);
        }

        private LevelUpRewardSlot GetInactiveSlot()
        {
            foreach (LevelUpRewardSlot slot in levelUpRewardSlots)
            {
                if (!slot.gameObject.activeSelf)
                {
                    return slot;
                }
            }

            return null;
        }
        private void Init(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateSlot();
            }
        }
        
        private LevelUpRewardSlot CreateSlot()
        {
            LevelUpRewardSlot slot = Instantiate(levelUpRewardSlot, transform);
            slot.gameObject.SetActive(false);
            levelUpRewardSlots.Add(slot);

            return slot;
        }
        
        private void HideAllSlot()
        {
            foreach (LevelUpRewardSlot slot in levelUpRewardSlots)
            {
                slot.gameObject.SetActive(false);
            }
        }
        
    }
}