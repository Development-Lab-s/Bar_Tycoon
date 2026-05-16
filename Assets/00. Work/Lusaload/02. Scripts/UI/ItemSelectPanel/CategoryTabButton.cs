using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI.ItemSelectPanel
{
    // 탭 버튼의 선택 상태에 따라 스프라이트와 크기를 변환하는 컴포넌트
    public class CategoryTabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Sprite normalSprite;    // 비선택 상태 스프라이트
        [SerializeField] private Sprite selectedSprite;  // 선택 상태 스프라이트
        [SerializeField] private float selectedScale = 1.2f;   // 선택 시 확대 배율
        [SerializeField] private float animDuration = 0.15f;   // 크기 전환 시간(초)

        private Image _image;
        private Coroutine _scaleCoroutine;
        private bool _isSelected;

        private void Awake()
        {
            _image = GetComponent<Image>();
            // Button의 색상 전환이 커스텀 비주얼을 덮지 않도록 전환 방식 비활성화
            var btn = GetComponent<Button>();
            if (btn != null) btn.transition = Selectable.Transition.None;
        }

        // 선택 여부에 따라 스프라이트 교체 및 스케일 애니메이션 실행
        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;

            if (_image != null)
            {
                // 할당된 스프라이트만 교체 (null이면 기존 이미지 유지)
                Sprite next = isSelected ? selectedSprite : normalSprite;
                if (next != null) _image.sprite = next;
            }

            float targetScale = isSelected ? selectedScale : 1f;
            if (_scaleCoroutine != null)
                StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleTo(targetScale));
        }

        // 호버 진입 시 외부 요인에 의한 스케일 변화를 차단하고 선택 상태 고정
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_scaleCoroutine != null)
                StopCoroutine(_scaleCoroutine);
            transform.localScale = Vector3.one * (_isSelected ? selectedScale : 1f);
        }

        // 호버 이탈 시 선택 상태에 맞는 스케일 복원
        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * (_isSelected ? selectedScale : 1f);
        }

        // 현재 스케일에서 목표 스케일로 선형 보간
        private IEnumerator ScaleTo(float target)
        {
            Vector3 from = transform.localScale;
            Vector3 to = Vector3.one * target;
            float elapsed = 0f;

            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(from, to, elapsed / animDuration);
                yield return null;
            }

            transform.localScale = to;
        }
    }
}
