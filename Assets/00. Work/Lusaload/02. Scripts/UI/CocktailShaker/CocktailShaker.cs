using System;
using _00._Work.Lusaload._02._Scripts.SO;
using _00._Work.Lusaload._02._Scripts.UI.AlcoholList;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI.CocktailShaker
{
    // 재료 드롭을 받아 시퀀스에 추가하고, 셰이킹 애니메이션 및 성공/실패 이벤트를 처리하는 셰이커 컴포넌트
    public class CocktailShaker : MonoBehaviour, IDropHandler, ISequenceReaderReceiver, IShakerNotifier
    {
        [SerializeField] private RectTransform panelRectTransform; // 셰이커 패널 RectTransform (드롭 완료 시 아래로 이동)
        [SerializeField] private Button shakeButton;               // 재료가 모두 채워졌을 때 표시되는 셰이크 버튼
        [SerializeField] private float panelDropDuration = 0.5f;  // 패널이 아래로 내려가는 애니메이션 시간(초)
        [SerializeField] private float shakeDuration = 1.5f;      // 셰이킹 애니메이션 지속 시간(초)

        public event Action OnShakerFull;      // 모든 슬롯이 채워졌을 때 발행 (SelectObjectSlider 잠금 등에 사용)
        public event Action OnCocktailSuccess; // 셰이킹 완료 후 성공 판정 시 발행
        public event Action OnCocktailFail;    // 셰이킹 완료 후 실패 판정 시 발행

        private ISequenceReader _sequenceReader; // 현재 시퀀스를 읽어오는 의존성
        private bool _isShaking;                 // 셰이킹 중복 실행 방지 플래그
        private MotionHandle _shakeHandle;       // 셰이킹 애니메이션 핸들 (취소 시 사용)

        // 시퀀스 리더를 교체하고 이전 리더의 이벤트 구독을 해제 후 새 리더에 구독
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

            if (_shakeHandle.IsActive()) _shakeHandle.Cancel();
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

        // 드래그 아이템이 셰이커 위에 드롭될 때 호출. 재료 추가를 시도하고 아이템을 원위치로 복귀
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

        // 드롭된 재료를 시퀀스에 추가하고, 셰이커가 가득 찼으면 셰이크 버튼을 활성화
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

        // AddAlcoholResult에 따라 로그를 출력하고, 완성 또는 가득 참 결과이면 패널 드롭 애니메이션 실행
        private void HandleResult(BaseAlcoholDataSO alcohol, AddAlcoholResult result)
        {
            switch (result)
            {
                case AddAlcoholResult.Added:
                    Debug.Log($"{alcohol.alcoholName} 추가됨");
                    break;
                case AddAlcoholResult.AddedAfterFail:
                    Debug.Log($"{alcohol.alcoholName} 추가됨 (실패 상태)");
                    break;
                case AddAlcoholResult.AlreadyContained:
                    Debug.Log($"{alcohol.alcoholName} 은(는) 이미 셰이커에 들어가 있습니다.");
                    break;
                case AddAlcoholResult.WrongIngredient:
                    Debug.Log($"실패: {alcohol.alcoholName} 은(는) 레시피에 없는 재료입니다.");
                    break;
                case AddAlcoholResult.WrongOrder:
                    Debug.Log($"실패: {alcohol.alcoholName} 은(는) 순서가 맞지 않습니다.");
                    break;
                case AddAlcoholResult.Completed:
                    Debug.Log($"{alcohol.alcoholName} 으로 레시피 완성!");
                    PlayAnimation();
                    break;
                case AddAlcoholResult.FullAfterFail:
                    Debug.Log($"{alcohol.alcoholName} 추가됨. 재료는 모두 채워졌지만 실패 상태입니다.");
                    PlayAnimation();
                    break;
                case AddAlcoholResult.Full:
                    Debug.Log("셰이커가 가득 찼습니다.");
                    break;
            }
            ItemPlusAnimation();
        }

        // 셰이커에 알콜이 추가될 때, 실행될 애니메이션
        private void ItemPlusAnimation()
        {
            LMotion.Create(Vector3.one, Vector3.one * 1.15f, 0.15f)
                .WithEase(Ease.OutQuad)
                .WithLoops(2, LoopType.Yoyo)
                .BindToLocalScale(GetComponent<RectTransform>());
        }

        // 셰이크 버튼을 표시하고 OnShakerFull 이벤트를 발행
        private void ActivateShakeButton()
        {
            if (shakeButton != null && !shakeButton.gameObject.activeSelf)
                shakeButton.gameObject.SetActive(true);

            OnShakerFull?.Invoke();
        }

        // 새 시퀀스 시작 시 셰이크 버튼을 숨기고 진행 중인 셰이킹 애니메이션을 취소
        private void ResetShakerUI()
        {
            if (shakeButton != null)
                shakeButton.gameObject.SetActive(false);

            if (_shakeHandle.IsActive()) _shakeHandle.Cancel();
            _isShaking = false;
        }

        // 셰이커 패널을 500px 아래로 이동시키는 드롭 연출 애니메이션
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

        // 흔들기 시작 시점의 시퀀스 상태를 캡처해 결과 이벤트에 사용
        private void PlayShakeAnimation()
        {
            _isShaking = true;
            bool isFail = _sequenceReader?.CurrentSequence?.State == SequenceState.Failed;
            Vector3 startPos = transform.localPosition;

            _shakeHandle = LMotion.Create(0f, shakeDuration, shakeDuration)
                .WithOnComplete(() =>
                {
                    transform.localPosition = startPos;
                    _isShaking = false;
                    if (isFail) OnCocktailFail?.Invoke();
                    else        OnCocktailSuccess?.Invoke();
                })
                .Bind(elapsed =>
                {
                    float noise = Mathf.PerlinNoise(elapsed * 10f, 0f) * 100f - 10f;
                    transform.localPosition = startPos + Vector3.up * noise;
                });
        }
    }
}
