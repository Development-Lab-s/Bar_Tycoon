using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Letterbox
{
    /// <summary>
    /// 레터박스 패널(좌/우)에 부착하는 클릭 차단기.
    /// UI가 숨겨진 상태일 때만 UI를 복구합니다.
    /// 모달(로그/스킵)이 열려 있거나 UI가 보이는 상태면 아무 동작도 하지 않습니다.
    /// 대사 진행(RequestAdvance)은 절대 호출하지 않습니다.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class StoryLetterboxInputBlocker : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private StoryCoreFacade facade;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (facade == null || !facade.IsOpen)
                return;

            if (facade.IsSkipPopupOpen || facade.IsLogOpen)
                return;

            if (facade.Session is { IsUiHidden: true })
                facade.ToggleUiVisible();

            // UI가 보이는 상태면 아무 동작 안 함 — 대사 진행 없음
        }
    }
}
