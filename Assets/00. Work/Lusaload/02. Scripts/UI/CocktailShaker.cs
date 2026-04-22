using System;
using System.Collections;
using System.Collections.Generic;
using _00._Work.Lusaload._02._Scripts.SO;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI
{
    public class CocktailShaker : MonoBehaviour, IDropHandler
    {
        [SerializeField] private CocktailRecipeSO cocktailRecipe;
        [SerializeField] private RectTransform panelRectTransform;
        [SerializeField] private Button shakeButton;
        [SerializeField] private float panelDropDuration = 0.5f;
        [SerializeField] private float shakeDuration = 1.5f;
        
        private readonly List<BaseAlcoholDataSO> _currentAlcoholList = new();
        private bool _isShaking;

        private void Start()
        {
            if (shakeButton != null)
            {
                shakeButton.onClick.AddListener(OnShakeButtonClicked);
                shakeButton.gameObject.SetActive(false);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            // 드래그해서 올린 버튼의 정보를 가져옵니다
            BaseAlcoholButtonUI alcoholItemUI = eventData.pointerDrag.GetComponent<BaseAlcoholButtonUI>();
            DraggableItem draggableItem = eventData.pointerDrag.GetComponent<DraggableItem>();

            AddAlcohol(alcoholItemUI.Data);
            
            if (draggableItem != null)
                draggableItem.ReturnToOriginalParent();
        }

        private void AddAlcohol(BaseAlcoholDataSO alcohol)
        {
            if (cocktailRecipe == null)
            {
                Debug.LogWarning("셰이커에 CocktailRecipeSO가 연결되지 않았습니다.");
                return;
            }

            if (cocktailRecipe.cocktailRecipeList == null || cocktailRecipe.cocktailRecipeList.Count == 0)
            {
                Debug.LogWarning("레시피 안에 재료가 비어 있습니다.");
                return;
            }

            if (_currentAlcoholList.Contains(alcohol))
            {
                Debug.Log($"{alcohol.alcoholName} 은(는) 이미 셰이커에 들어가 있습니다.");
                return;
            }
            
            if (!cocktailRecipe.cocktailRecipeList.Contains(alcohol))
            {
                Debug.Log($"{alcohol.alcoholName} 은(는) {cocktailRecipe.cocktailName} 레시피에 없는 재료입니다.");
                return;
            }
            
            _currentAlcoholList.Add(alcohol);
            Debug.Log($"{alcohol.alcoholName} 추가됨");

            // 셰이커 안에 레시피 내, 모든 술이 들어 있는지 확인
            if (IsRecipeCompleted())
            {
                CompleteCocktailMake();
            }
        }

        private bool IsRecipeCompleted()
        {
            if (cocktailRecipe == null || cocktailRecipe.cocktailRecipeList == null)
                return false;

            if (_currentAlcoholList.Count != cocktailRecipe.cocktailRecipeList.Count)
                return false;
            
            // 칵테일 레시피의 리스트를 전부 순회하여 모든 요소가 셰이커 안에 존재할 경우를 파악합니다.
            foreach (BaseAlcoholDataSO requiredAlcohol in cocktailRecipe.cocktailRecipeList)
            {
                if (requiredAlcohol == null)
                    continue;

                if (!_currentAlcoholList.Contains(requiredAlcohol))
                    return false;
            }

            return true;
        }
        
        private void CompleteCocktailMake()
        {
            if (panelRectTransform == null || shakeButton == null)
                return;
 
            shakeButton.gameObject.SetActive(true);
 
            Vector3 startPosition = panelRectTransform.anchoredPosition;
            Vector3 endPosition = startPosition + Vector3.down * 500;
 
            LMotion.Create(startPosition, endPosition, panelDropDuration)
                .WithEase(Ease.OutCubic)
                .Bind(position => panelRectTransform.anchoredPosition = position);
        }
        
        private void OnShakeButtonClicked()
        {
            if (_isShaking)
                return;
 
            PlayShakeAnimation();
        }

        private void PlayShakeAnimation()
        {
            _isShaking = true;
            StartCoroutine(ShakeCoroutine());
        }
        
        private IEnumerator ShakeCoroutine()
        {
            float elapsedTime = 0f;
            var startPosition = transform.localPosition;
    
            while (elapsedTime < shakeDuration)
            {
                elapsedTime += Time.deltaTime;

                float shakeAmount = Mathf.PerlinNoise(elapsedTime * 10f, 0f) * 100f - 10f;
                transform.localPosition = startPosition + Vector3.up * shakeAmount;
        
                yield return null;
            }
    
            transform.localPosition = startPosition;
            _isShaking = false;
        }
    }
}