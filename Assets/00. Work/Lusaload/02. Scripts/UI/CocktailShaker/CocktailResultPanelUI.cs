using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    public class CocktailResultPanelUI : MonoBehaviour
    {
        [SerializeField] private CocktailShaker cocktailShaker;

        [Header("Success")]
        [SerializeField] private GameObject successPanel;
        [SerializeField] private TextMeshProUGUI successText;
        [SerializeField] private Button successCloseButton;

        [Header("Fail")]
        [SerializeField] private GameObject failPanel;
        [SerializeField] private TextMeshProUGUI failText;
        [SerializeField] private Button failCloseButton;

        private void Awake()
        {
            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (cocktailShaker != null)
            {
                cocktailShaker.OnCocktailSuccess += HandleSuccess;
                cocktailShaker.OnCocktailFail += HandleFail;
            }

            if (successCloseButton != null)
                successCloseButton.onClick.AddListener(ClosePanels);

            if (failCloseButton != null)
                failCloseButton.onClick.AddListener(ClosePanels);
        }

        private void OnDisable()
        {
            if (cocktailShaker != null)
            {
                cocktailShaker.OnCocktailSuccess -= HandleSuccess;
                cocktailShaker.OnCocktailFail -= HandleFail;
            }

            if (successCloseButton != null)
                successCloseButton.onClick.RemoveListener(ClosePanels);

            if (failCloseButton != null)
                failCloseButton.onClick.RemoveListener(ClosePanels);
        }

        private void HandleSuccess()
        {
            if (failPanel != null) failPanel.SetActive(false);
            if (successPanel == null) return;

            if (successText != null)
                successText.text = "성공";

            successPanel.SetActive(true);
        }

        private void HandleFail()
        {
            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel == null) return;

            if (failText != null)
                failText.text = "실패...";

            failPanel.SetActive(true)
;
        }
        public void ClosePanels()
        {
            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(false);
        }
    }
}