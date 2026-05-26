using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _00._Work.Lusaload._02._Scripts.UI.AlcoholList;

namespace _00._Work.Lusaload._02._Scripts
{
    // 드래그 즉시 고스트를 생성해 커서를 따라다니게 하는 컴포넌트
    // 스크롤은 리스트의 빈 영역에서 가능
    public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private float ghostAlpha = 0.9f;       // 커서를 따라다니는 고스트 투명도
        [SerializeField] private float originFadeAlpha = 0.35f; // 드래그 중 원본 아이템 투명도
        [SerializeField] private float dragScale = 1.1f;        // 고스트 픽업 스케일 배율
        [SerializeField] private float pickupDuration = 0.12f;  // 고스트 픽업 애니메이션 시간
        [SerializeField] private float releaseDuration = 0.2f;  // 원본 복귀 애니메이션 시간

        private Canvas _rootCanvas;           // 고스트를 최상위로 올리기 위한 루트 캔버스
        private CanvasGroup _canvasGroup;     // 원본 투명도 제어
        private RectTransform _rectTransform; // 원본 위치 참조

        private GameObject _dragGhost; // 커서를 따라다니는 시각적 복사본
        private RectTransform _ghostRT; // OnDrag 성능을 위해 캐시
        private Vector2 _grabOffset;    // 클릭 지점과 고스트 피벗 사이의 오프셋

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _rootCanvas = GetComponentInParent<Canvas>();
        }

        // 고스트 방식에서는 원본이 제자리 유지 — CocktailShaker 호환을 위해 존재
        public void ReturnToOriginalParent() { }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 원본은 반투명 ghost 상태로 제자리 유지 (레이아웃 변형 없음)
            _canvasGroup.alpha = originFadeAlpha;

            // 시각적 복사본 생성 후 그랩 오프셋 계산
            _dragGhost = CreateDragGhost();
            _grabOffset = (Vector2)_ghostRT.position - eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghostRT == null) return;

            _ghostRT.position = eventData.position + _grabOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost);
                _dragGhost = null;
                _ghostRT = null;
            }

            PlayReleaseAnimation();
        }

        // 전체 하이어라키 복사 후 상호작용 컴포넌트만 제거해 시각적 정확도 확보
        private GameObject CreateDragGhost()
        {
            GameObject ghost = Instantiate(gameObject, _rootCanvas.transform);

            // 데이터·상호작용 컴포넌트 제거 (시각 전용 오브젝트로 만들기)
            if (ghost.TryGetComponent<DraggableItem>(out var di)) Destroy(di);
            if (ghost.TryGetComponent<BaseAlcoholButtonUI>(out var ui)) Destroy(ui);

            // 고스트 아래의 IDropHandler가 드롭 이벤트를 받을 수 있도록 레이캐스트 차단 해제
            if (!ghost.TryGetComponent<CanvasGroup>(out var cg))
                cg = ghost.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = ghostAlpha;

            // 원본 위치에 배치 후 픽업 스케일 애니메이션
            _ghostRT = ghost.GetComponent<RectTransform>();
            _ghostRT.position = _rectTransform.position;

            LMotion.Create(Vector3.one, Vector3.one * dragScale, pickupDuration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(_ghostRT);

            return ghost;
        }

        // 드래그 완료 후 원본 투명도를 부드럽게 복원
        private void PlayReleaseAnimation()
        {
            LMotion.Create(_canvasGroup.alpha, 1f, releaseDuration)
                .WithEase(Ease.OutQuad)
                .Bind(a => _canvasGroup.alpha = a);
        }
    }
}
