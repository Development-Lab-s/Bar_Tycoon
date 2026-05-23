using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Contracts;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Types;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Input
{
    /// <summary>
    /// 스토리 시스템에서 사용자 입력을 처리하는 라우터 클래스입니다.
    /// 이 클래스는 스토리 코어와 텍스트 디렉터, 선택지 패널 컨트롤러 등
    /// 스토리 시스템의 주요 컴포넌트에 대한 참조를 가지고 있으며,
    /// 사용자 입력 이벤트(탭, 자동 진행 토글, UI 표시 토글, 로그 열기/닫기,
    /// 스킵 요청, 종료 요청 등)를 처리하여 스토리 시스템의 상태를 적절히 업데이트하는 역할을 합니다.
    /// </summary>
    
    public sealed class StoryInputRouter : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private StoryCoreFacade facade;

        [Header("Directors")]
        [SerializeField] private MonoBehaviour textDirectorSource;
        [SerializeField] private MonoBehaviour choicePanelSource;

        private ITextDirector _textDirector;
        private IChoicePanelController _choicePanel;

        private void Awake()
        {
            _textDirector = textDirectorSource as ITextDirector;
            _choicePanel = choicePanelSource as IChoicePanelController;

            Debug.Assert(facade != null, "StoryCoreFacade reference is missing.");
            Debug.Assert(_textDirector != null, "ITextDirector implementation is missing.");
            Debug.Assert(_choicePanel != null, "IChoicePanelController implementation is missing.");
        }

        public void OnTap()
        {
            if (facade == null || !facade.IsOpen)
                return;
            
            if (facade.IsSkipPopupOpen)
                return;
            
            if (facade.IsLogOpen)
                return;

            if (facade.Session is { IsUiHidden: true })
            {
                facade.ToggleUiVisible();
                return;
            }

            if (_choicePanel is { IsRevealing: true })
            {
                _choicePanel.CompleteReveal();
                return;
            }

            if (_choicePanel is { IsChoiceOpen: true })
                return;

            if (_textDirector is { IsTyping: true })
            {
                _textDirector.CompleteCurrentLine();
                return;
            }

            facade.RequestAdvance();
        }

        public void OnToggleAuto(bool toggleEnabled)
        {
            if (facade == null || facade.IsSkipPopupOpen)
                return;

            facade.SetAutoMode(toggleEnabled);
        }

        public void OnToggleUiVisible()
        {
            if (facade == null || !facade.IsOpen)
                return;

            // 모달이 열려 있으면 숨기기 요청 무시
            if (facade.IsSkipPopupOpen || facade.IsLogOpen)
                return;

            // 선택지가 열려 있으면 숨기기 요청 무시
            if (_choicePanel is { IsChoiceOpen: true })
                return;

            facade.ToggleUiVisible();
        }

        public void OnOpenLog()
        {
            if (facade == null || !facade.IsOpen || facade.IsSkipPopupOpen)
                return;

            facade.OpenLog();
        }

        public void OnCloseLog()
        {
            if (facade == null || !facade.IsOpen)
                return;

            facade.CloseLog();
        }

        public void OnSkipPressed()
        {
            if (facade == null || !facade.IsRunning)
                return;

            facade.OpenSkipSummary();
        }

        public void OnClosePressed()
        {
            if (facade == null || !facade.IsRunning)
                return;

            facade.RequestClose(StoryCloseReason.UserClosed);
        }
    }
}