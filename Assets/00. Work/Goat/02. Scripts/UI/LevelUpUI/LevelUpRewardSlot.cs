using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    public class LevelUpRewardSlot : MonoBehaviour
    {
        [SerializeField] private Image image;

        public void SetImage(Sprite sprite)
        {
            image.sprite = sprite;
        }
    }
}