using LitMotion;
using LitMotion.Extensions;
using _00._Work.Lusaload._02._Scripts.UI.CocktailShaker;
using UnityEngine;
using UnityEngine.UI;

namespace _00._Work.Lusaload._02._Scripts.UI
{
    // SlideButton 클릭 시 SelectObject를 화면 아래로 숨기거나 다시 올리는 토글 컴포넌트
    public class SelectObjectSlider : MonoBehaviour
    {
        [SerializeField] private Button slideButton;            // 슬라이드 토글 버튼
        [SerializeField] private RectTransform selectObject;   // 슬라이드할 패널 RectTransform
        [SerializeField] private RectTransform category;       // 함께 슬라이드할 카테고리 RectTransform
        [SerializeField] private float duration = 0.4f;        // 슬라이드 애니메이션 지속 시간(초)
        [SerializeField] private Ease easeDown = Ease.InCubic;  // 내려갈 때 이징
        [SerializeField] private Ease easeUp = Ease.OutCubic;   // 올라올 때 이징

        [SerializeField] private float shownY = -366.1f;  // 보이는 위치 anchoredPosition.y
        [SerializeField] private float hiddenYCorrection; // 숨겨지는 위치에 대한 보정값
        
        [SerializeField] private bool startHidden = true;  // 시작 시 숨김 여부

        [Header("Shaker Integration")]
        [SerializeField] private MonoBehaviour shakerSourceMB;  // IShakerNotifier 구현체를 할당

        private IShakerNotifier _shakerNotifier; // 캐스팅된 IShakerNotifier 참조

        [Header("Button Icon Flip")] 
        private RectTransform _buttonIcon;  // 회전시킬 버튼 아이콘 Transform

        private float _hiddenY;           // 계산된 숨김 위치 anchoredPosition.y
        private bool _isHidden = true;
        private bool _locked;             // true일 때 SlideUp 불가 (셰이커 완성 상태)
        private MotionHandle _handle;     // selectObject 슬라이드 애니메이션 핸들
        private MotionHandle _rotHandle;  // 버튼 아이콘 회전 애니메이션 핸들

        private RectTransform _categoryRect;   // category 캐시 (null이면 애니메이션 생략)
        private float _categoryShownY;         // category 보이는 위치
        private float _categoryHiddenY;        // category 숨겨지는 위치
        private MotionHandle _catHandle;       // category 슬라이드 애니메이션 핸들

        private void Awake()
        {
            _shakerNotifier = shakerSourceMB as IShakerNotifier;
            if (_shakerNotifier != null)
                _shakerNotifier.OnShakerFull += LockAndSlideDown;
        }

        private void Start()
        {
            // 오브젝트가 완전히 캔버스 하단 밖으로 나가는 y 좌표
            var canvasRect = selectObject.GetComponentInParent<Canvas>()
                                         .GetComponent<RectTransform>();
            _hiddenY = -(canvasRect.rect.height * 0.5f + selectObject.rect.height * 0.5f) + hiddenYCorrection;

            // 위치 초기화를 생략하므로 실제 시각 상태는 항상 shown — 상태를 맞춤
            _isHidden = false;

            if (_buttonIcon != null)
                _buttonIcon.localEulerAngles = new Vector3(0f, 0f, 0f);

            if (category != null)
            {
                _categoryRect = category;
                _categoryShownY = _categoryRect.anchoredPosition.y;
                _categoryHiddenY = -(canvasRect.rect.height * 0.5f + _categoryRect.rect.height * 0.5f);

                // var catPos = _categoryRect.anchoredPosition;
                // catPos.y = _isHidden ? _categoryHiddenY : _categoryShownY;
                // _categoryRect.anchoredPosition = catPos;
            }

            _buttonIcon = slideButton.gameObject.GetComponent<RectTransform>();
            slideButton.onClick.AddListener(Toggle);
        }

        private void OnDestroy()
        {
            if (_shakerNotifier != null)
                _shakerNotifier.OnShakerFull -= LockAndSlideDown;
        }

        // 셰이커 완성 시 호출 — 잠금 후 SlideDown
        private void LockAndSlideDown()
        {
            _locked = true;
            SlideDown();
        }

        // 잠금 해제 — 새 시퀀스 시작 등 외부에서 호출
        public void Unlock()
        {
            _locked = false;
        }

        // 현재 상태에 따라 올리거나 내림 (잠금 상태면 SlideUp 불가)
        public void Toggle()
        {
            if (_isHidden)
            {
                if (!_locked) SlideUp();
            }
            else
            {
                SlideDown();
            }
        }

        // 패널을 화면 아래로 슬라이드 아웃, 아이콘을 180도로 뒤집음
        public void SlideDown()
        {
            _isHidden = true;
            Animate(_hiddenY, easeDown);
            AnimateCategory(_categoryHiddenY, easeDown);
            RotateIcon(180f);
        }

        // 패널을 화면 안으로 슬라이드 인, 아이콘을 0도로 복원 (잠금 상태면 무시)
        public void SlideUp()
        {
            if (_locked) return;
            _isHidden = false;
            Animate(shownY, easeUp);
            AnimateCategory(_categoryShownY, easeUp);
            RotateIcon(0f);
        }

        private void Animate(float targetY, Ease ease)
        {
            if (_handle.IsActive()) _handle.Cancel();
            _handle = LMotion.Create(selectObject.anchoredPosition.y, targetY, duration)
                .WithEase(ease)
                .BindToAnchoredPositionY(selectObject);
        }

        // category를 목표 Y 위치로 애니메이션 (null이면 무시)
        private void AnimateCategory(float targetY, Ease ease)
        {
            if (_categoryRect == null) return;
            if (_catHandle.IsActive()) _catHandle.Cancel();
            _catHandle = LMotion.Create(_categoryRect.anchoredPosition.y, targetY, duration)
                .WithEase(ease)
                .BindToAnchoredPositionY(_categoryRect);
        }

        // 아이콘 Z축 회전 애니메이션 (슬라이드와 같은 duration 사용)
        private void RotateIcon(float targetZ)
        {
            if (_buttonIcon == null) return;
            if (_rotHandle.IsActive()) _rotHandle.Cancel();
            float fromZ = _buttonIcon.localEulerAngles.z;
            _rotHandle = LMotion.Create(fromZ, targetZ, duration)
                .WithEase(easeUp)
                .Bind(z => _buttonIcon.localEulerAngles = new Vector3(0f, 0f, z));
        }
    }
}
