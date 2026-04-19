using System;
using System.Collections;
using _00._Work.Lusaload._02._Scripts.SO;
using _00._Work.Lusaload._02._Scripts.UI.AlcoholList;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    public class CocktailShaker : MonoBehaviour, IDropHandler, ISequenceReaderReceiver
    {
        [SerializeField] private RectTransform panelRectTransform;
        [SerializeField] private Button shakeButton;
        [SerializeField] private float panelDropDuration = 0.5f;
        [SerializeField] private float shakeDuration = 1.5f;

        public event Action OnCocktailSuccess;
        public event Action OnCocktailFail;

        private ISequenceReader _sequenceReader;
        private bool _isShaking;
        private bool _isFailState;

        public void SetSequenceReader(ISequenceReader reader)
        {
            if (_sequenceReader != null)
                _sequenceReader.OnSequenceChanged -= OnSequenceChanged;

            _sequenceReader = reader;

            if (_sequenceReader != null)
                _sequenceReader.OnSequenceChanged += OnSequenceChanged;
        }

        private void OnDestroy()
        {
            if (_sequenceReader != null)
                _sequenceReader.OnSequenceChanged -= OnSequenceChanged;
        }

        private void OnSequenceChanged(CocktailOrderSequence sequence)
        {
            ResetShakerUI();
        }

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

            BaseAlcoholButtonUI alcoholItemUI = eventData.pointerDrag.GetComponent<BaseAlcoholButtonUI>();
            DraggableItem draggableItem = eventData.pointerDrag.GetComponent<DraggableItem>();

            if (alcoholItemUI != null)
                HandleAlcoholDropped(alcoholItemUI.Data);

            if (draggableItem != null)
                draggableItem.ReturnToOriginalParent();
        }

        private void HandleAlcoholDropped(BaseAlcoholDataSO alcohol)
        {
            CocktailOrderSequence sequence = _sequenceReader?.CurrentSequence;
            if (sequence == null)
            {
                Debug.LogWarning("셰이커에 시퀀스가 설정되지 않았습니다.");
                return;
            }

            AddAlcoholResult result = sequence.TryAdd(alcohol);
            HandleResult(alcohol, result);

            if (sequence.IsFull)
                ActivateShakeButton();
        }

        private void HandleResult(BaseAlcoholDataSO alcohol, AddAlcoholResult result)
        {
            switch (result)
            {
                case AddAlcoholResult.Added:
                    Debug.Log($"{alcohol.alcoholName} 추가됨");
                    break;

                case AddAlcoholResult.AddedAfterFail:
                    Debug.Log($"{alcohol.alcoholName} 추가됨 (실패 상태)");
                    _isFailState = true;
                    break;

                case AddAlcoholResult.AlreadyContained:
                    Debug.Log($"{alcohol.alcoholName} 은(는) 이미 셰이커에 들어가 있습니다.");
                    break;

                case AddAlcoholResult.WrongIngredient:
                    Debug.Log($"실패: {alcohol.alcoholName} 은(는) 레시피에 없는 재료입니다.");
                    _isFailState = true;
                    break;

                case AddAlcoholResult.WrongOrder:
                    Debug.Log($"실패: {alcohol.alcoholName} 은(는) 순서가 맞지 않습니다.");
                    _isFailState = true;
                    break;

                case AddAlcoholResult.Completed:
                    Debug.Log($"{alcohol.alcoholName} 으로 레시피 완성!");
                    _isFailState = false;
                    PlayAnimation();
                    break;

                case AddAlcoholResult.FullAfterFail:
                    Debug.Log($"{alcohol.alcoholName} 추가됨. 재료는 모두 채워졌지만 실패 상태입니다.");
                    _isFailState = true;
                    PlayAnimation();
                    break;

                case AddAlcoholResult.Full:
                    Debug.Log("셰이커가 가득 찼습니다.");
                    break;
            }
        }

        private void ActivateShakeButton()
        {
            if (shakeButton != null && !shakeButton.gameObject.activeSelf)
                shakeButton.gameObject.SetActive(true);
        }

        private void ResetShakerUI()
        {
            if (shakeButton != null)
                shakeButton.gameObject.SetActive(false);

            _isFailState = false;
            _isShaking = false;
        }

        private void PlayAnimation()
        {
            if (panelRectTransform == null) return;

            Vector3 startPosition = panelRectTransform.anchoredPosition;
            Vector3 endPosition = startPosition + Vector3.down * 500;

            LMotion.Create(startPosition, endPosition, panelDropDuration)
                .WithEase(Ease.OutCubic)
                .Bind(position => panelRectTransform.anchoredPosition = position);
        }

        private void OnShakeButtonClicked()
        {
            if (_isShaking) return;
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
            Vector3 startPosition = transform.localPosition;

            while (elapsedTime < shakeDuration)
            {
                elapsedTime += Time.deltaTime;

                float shakeAmount = Mathf.PerlinNoise(elapsedTime * 10f, 0f) * 100f - 10f;
                transform.localPosition = startPosition + Vector3.up * shakeAmount;

                yield return null;
            }

            transform.localPosition = startPosition;
            _isShaking = false;

            RaiseResultEvent();
        }

        private void RaiseResultEvent()
        {
            if (_isFailState)
                OnCocktailFail?.Invoke();
            else
                OnCocktailSuccess?.Invoke();
        }
    }
}