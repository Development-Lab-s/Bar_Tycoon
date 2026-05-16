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
        [SerializeField] private Button slideButton;
        [SerializeField] private RectTransform selectObject;
        [SerializeField] private RectTransform category;
        [SerializeField] private float duration = 0.4f;
        [SerializeField] private Ease easeDown = Ease.InCubic;  // 내려갈 때 이징
        [SerializeField] private Ease easeUp = Ease.OutCubic;   // 올라올 때 이징

        [SerializeField] private float shownY = -366.1f;  // 보이는 위치 anchoredPosition.y
        [SerializeField] private float hiddenYCorrection; // 숨겨지는 위치에 대한 보정값
        
        [SerializeField] private bool startHidden = true;  // 시작 시 숨김 여부

        [Header("Shaker Integration")]
        [SerializeField] private MonoBehaviour shakerSourceMB;  // IShakerNotifier 구현체를 할당

        private IShakerNotifier _shakerNotifier;

        [Header("Button Icon Flip")]
        [SerializeField] private RectTransform buttonIcon;  // 회전시킬 버튼 아이콘 Transform

        private float _hiddenY;
        private bool _isHidden = true;
        private bool _locked;  // true일 때 SlideUp 불가 (셰이커 완성 상태)
        private MotionHandle _handle;
        private MotionHandle _rotHandle;

        private RectTransform _categoryRect;
        private float _categoryShownY;
        private float _categoryHiddenY;
        private MotionHandle _catHandle;

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

            _isHidden = startHidden;

            // 초기 위치·회전을 즉시 적용
            // var pos = selectObject.anchoredPosition;
            // pos.y = _isHidden ? _hiddenY : shownY;
            // selectObject.anchoredPosition = pos;

            if (buttonIcon != null)
                buttonIcon.localEulerAngles = new Vector3(0f, 0f, _isHidden ? 180f : 0f);

            if (category != null)
            {
                _categoryRect = category;
                _categoryShownY = _categoryRect.anchoredPosition.y;
                _categoryHiddenY = -(canvasRect.rect.height * 0.5f + _categoryRect.rect.height * 0.5f);

                var catPos = _categoryRect.anchoredPosition;
                catPos.y = _isHidden ? _categoryHiddenY : _categoryShownY;
                _categoryRect.anchoredPosition = catPos;
            }

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
            if (buttonIcon == null) return;
            if (_rotHandle.IsActive()) _rotHandle.Cancel();
            float fromZ = buttonIcon.localEulerAngles.z;
            _rotHandle = LMotion.Create(fromZ, targetZ, duration)
                .WithEase(easeUp)
                .Bind(z => buttonIcon.localEulerAngles = new Vector3(0f, 0f, z));
        }
    }
}
