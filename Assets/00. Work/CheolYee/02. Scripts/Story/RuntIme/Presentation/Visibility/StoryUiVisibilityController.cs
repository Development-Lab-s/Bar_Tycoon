using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Motion;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Visibility
{
    /// <summary>
    /// TopBar / DialogueArea / ChoiceArea 의 시각적 숨김·표시를
    /// UIMotionPlayer를 통해 제어합니다.
    ///
    /// 세션 상태(IsUiHidden)는 StoryCoreFacade에서 관리합니다.
    /// 이 클래스는 "시각" 전담으로, 러너·세션·진행 상태에 영향을 주지 않습니다.
    ///
    /// CanvasGroup을 함께 연결하면 Hide 시 blocksRaycasts=false,
    /// Show 시 blocksRaycasts=true 로 자동 처리됩니다.
    /// </summary>
    public sealed class StoryUiVisibilityController : MonoBehaviour
    {
        [Header("TopBar")]
        [SerializeField] private UIMotionPlayer topBarMotion;
        [SerializeField] private CanvasGroup topBarCanvasGroup;

        [Header("DialogueArea")]
        [SerializeField] private UIMotionPlayer dialogueAreaMotion;
        [SerializeField] private CanvasGroup dialogueAreaCanvasGroup;

        [Header("ChoiceArea")]
        [SerializeField] private UIMotionPlayer choiceAreaMotion;
        [SerializeField] private CanvasGroup choiceAreaCanvasGroup;

        private const string ShowLabel = "Show";
        private const string HideLabel = "Hide";

        // ── Public API ─────────────────────────────

        /// <summary>Hide 애니메이션 재생. 완료 후 raycasts 차단 해제.</summary>
        public void Hide()
        {
            topBarMotion?.Play(HideLabel, onFinish: () => SetRaycasts(topBarCanvasGroup, false));
            dialogueAreaMotion?.Play(HideLabel, onFinish: () => SetRaycasts(dialogueAreaCanvasGroup, false));
            choiceAreaMotion?.Play(HideLabel, onFinish: () => SetRaycasts(choiceAreaCanvasGroup, false));
        }

        /// <summary>Show 애니메이션 재생. 즉시 raycasts 복구 후 fadeIn.</summary>
        public void Show()
        {
            SetRaycasts(topBarCanvasGroup, true);
            SetRaycasts(dialogueAreaCanvasGroup, true);
            SetRaycasts(choiceAreaCanvasGroup, true);

            topBarMotion?.Play(ShowLabel);
            dialogueAreaMotion?.Play(ShowLabel);
            choiceAreaMotion?.Play(ShowLabel);
        }

        /// <summary>모션 없이 즉시 숨김 상태 적용 (초기화·강제 리셋용).</summary>
        public void ApplyHiddenImmediate()
        {
            topBarMotion?.ApplyState(HideLabel);
            dialogueAreaMotion?.ApplyState(HideLabel);
            choiceAreaMotion?.ApplyState(HideLabel);

            SetRaycasts(topBarCanvasGroup, false);
            SetRaycasts(dialogueAreaCanvasGroup, false);
            SetRaycasts(choiceAreaCanvasGroup, false);
        }

        /// <summary>모션 없이 즉시 표시 상태 적용 (초기화·강제 리셋용).</summary>
        public void ApplyShownImmediate()
        {
            SetRaycasts(topBarCanvasGroup, true);
            SetRaycasts(dialogueAreaCanvasGroup, true);
            SetRaycasts(choiceAreaCanvasGroup, true);

            topBarMotion?.ApplyState(ShowLabel);
            dialogueAreaMotion?.ApplyState(ShowLabel);
            choiceAreaMotion?.ApplyState(ShowLabel);
        }

        // ── Helpers ────────────────────────────────

        private static void SetRaycasts(CanvasGroup cg, bool value)
        {
            if (cg == null) return;
            cg.blocksRaycasts = value;
            cg.interactable = value;
        }
    }
}
