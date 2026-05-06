using System;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.AchievementUI
{
    public class AchieveTopUI : MonoBehaviour
    {
        [SerializeField] private Button exitButton;
        [SerializeField] private Toggle isCompleteToggle;

        public event Action OnExitBtnClick;
        public event Action<bool> OnIsCompleteBtnClick;

        private void OnEnable()
        {
            exitButton.onClick.AddListener(HandleOnClickExitBtn);
            isCompleteToggle.onValueChanged.AddListener(HandleToggleChanged);
        }

        private void OnDisable()
        {
            exitButton.onClick.RemoveListener(HandleOnClickExitBtn);
            isCompleteToggle.onValueChanged.RemoveListener(HandleToggleChanged);
        }

        private void HandleOnClickExitBtn()
        {
            OnExitBtnClick?.Invoke();
        }

        private void HandleToggleChanged(bool isComplete)
        {
            OnIsCompleteBtnClick?.Invoke(isComplete);
        }
    }
}