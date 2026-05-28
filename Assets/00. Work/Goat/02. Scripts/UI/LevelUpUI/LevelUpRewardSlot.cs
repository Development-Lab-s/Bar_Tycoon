using Systems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _00._Work.Goat._02._Scripts.UI.LevelUpUI
{
    public class LevelUpRewardSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image image;
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private Vector2 panelOffset = new Vector2(20f, -20f);

        private TextMeshProUGUI descriptionText;
        private GameObject descriptionPanel;
        private RectTransform descriptionPanelRect;
        private RectTransform canvasRect;

        private string _description;
        private bool _isHovering;

        public void Init(GameObject panel, TextMeshProUGUI text)
        {
            descriptionPanel = panel;
            descriptionText = text;
            if (panel == null) return;
            descriptionPanelRect = panel.GetComponent<RectTransform>();
            var parentCanvas = panel.GetComponentInParent<Canvas>(true);
            canvasRect = parentCanvas != null ? parentCanvas.transform as RectTransform : null;
        }

        public void SetImage(Sprite sprite, string description)
        {
            image.sprite = sprite;
            _description = description;
        }

        private void Update()
        {
            if (!_isHovering || descriptionPanelRect == null)
                return;

            Vector2 localPoint;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                playerInput.MousePosition,
                null,
                out localPoint
            );

            descriptionPanelRect.anchoredPosition = localPoint + panelOffset;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_description))
                return;

            _isHovering = true;
            descriptionText.text = _description;
            descriptionPanel.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            descriptionPanel.SetActive(false);
        }
    }
}