using BBJ.Data;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BBJ.UI.Order
{
    public class FoodGroupCardUI : MonoBehaviour
    {
        [SerializeField] private Image    _foodIcon;
        [SerializeField] private TMP_Text _foodName;
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private Image    _occupiedBadge;
        [SerializeField] private Button   _button;

        private FoodDataSO         _food;
        private Action<FoodDataSO> _onClick;

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        public void Setup(FoodDataSO food, int count, bool isOccupied, Action<FoodDataSO> onClick)
        {
            _food    = food;
            _onClick = onClick;

            _foodIcon.sprite = food != null ? food.Icon : null;
            _foodName.text   = food != null ? food.FoodName : "-";

            Refresh(count, isOccupied);
        }

        public void Refresh(int count, bool isOccupied)
        {
            _occupiedBadge.gameObject.SetActive(isOccupied);
            _countLabel.gameObject.SetActive(!isOccupied);

            if (!isOccupied)
                _countLabel.text = $"x{count}";
        }

        private void OnClicked() => _onClick?.Invoke(_food);
    }
}
