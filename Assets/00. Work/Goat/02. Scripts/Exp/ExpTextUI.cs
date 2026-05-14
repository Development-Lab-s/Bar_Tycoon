using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.Exp
{
    public class ExpTextUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelText;

        public void LevelChange(int level)
        {
            levelText.text = level.ToString();
        }
    }
}