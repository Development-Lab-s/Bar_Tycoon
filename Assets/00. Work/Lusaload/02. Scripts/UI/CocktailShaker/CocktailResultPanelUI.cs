using _00._Work.Lusaload._02._Scripts.Recipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    // 셰이킹 결과(성공/실패)에 따라 결과 패널을 표시하는 UI 컴포넌트
    public class CocktailResultPanelUI : MonoBehaviour, IRecipeReaderReceiver
    {
        [SerializeField] private CocktailShaker cocktailShaker; // 성공·실패 이벤트를 구독할 셰이커

        [Header("Success")]
        [SerializeField] private GameObject successPanel;          // 성공 시 활성화할 패널
        [SerializeField] private TextMeshProUGUI successText;      // 성공 메시지 텍스트
        [SerializeField] private Image cocktailImage;              // 완성된 칵테일 이미지
        [SerializeField] private TextMeshProUGUI cocktailName;     // 완성된 칵테일 이름
        [SerializeField] private Button successCloseButton;        // 성공 패널 닫기 버튼

        [Header("Fail")]
        [SerializeField] private GameObject failPanel;             // 실패 시 활성화할 패널
        [SerializeField] private TextMeshProUGUI failText;         // 실패 메시지 텍스트
        [SerializeField] private Button failCloseButton;           // 실패 패널 닫기 버튼

        private IRecipeReader _recipeReader; // 현재 레시피 정보(아이콘 등)를 읽기 위한 의존성

        public void SetRecipeReader(IRecipeReader reader)
        {
            _recipeReader = reader;
        }

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

        // 성공 패널을 열고 현재 레시피의 칵테일 아이콘을 표시
        private void HandleSuccess()
        {
            if (failPanel != null) failPanel.SetActive(false);
            if (successPanel == null) return;

            if (cocktailName != null)
                cocktailName.text = _recipeReader?.CurrentRecipe?.cocktailName;

            if (successText != null)
                successText.text = "성공";

            if (cocktailImage != null)
                cocktailImage.sprite = _recipeReader?.CurrentRecipe?.cocktailIcon;

            successPanel.SetActive(true);
        }

        // 실패 패널을 열고 실패 메시지를 표시
        private void HandleFail()
        {
            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel == null) return;

            if (failText != null)
                failText.text = "실패...";

            failPanel.SetActive(true);
        }

        // 성공·실패 패널을 모두 닫음. 닫기 버튼 및 외부에서 호출 가능
        public void ClosePanels()
        {
            if (successPanel != null) successPanel.SetActive(false);
            if (failPanel != null) failPanel.SetActive(false);
        }
    }
}
