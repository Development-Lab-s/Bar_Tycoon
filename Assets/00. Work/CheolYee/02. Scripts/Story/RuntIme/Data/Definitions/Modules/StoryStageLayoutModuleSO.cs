using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    /// <summary>
    /// 라인 시점의 스테이지 전체 캐릭터 배치 상태를 정의하는 모듈.
    ///
    /// ■ Source of truth:
    ///   - 에디터 프리뷰에서 드래그/Inspector로 조정 → 이 SO에 저장
    ///   - 런타임에서 StoryStageLayoutModuleExecutor 가 읽어 IStoryStageDirector 에 전달
    ///   - 이전 라인 대비 enter / move / exit 자동 처리
    ///
    /// ■ 상태 누적 규칙:
    ///   - 이 모듈이 없는 라인: 이전 라인 스테이지 상태 그대로 유지
    ///   - 이 모듈이 있는 라인: actors 목록으로 스테이지를 갱신
    ///     · 목록에 없는 기존 캐릭터는 자동 퇴장(exit) 처리
    /// </summary>
    [StoryModuleMetadata("Stage Layout", category: "Stage", accentColorHex: "#9B59B6", sortPriority: 1)]
    [CreateAssetMenu(fileName = "StageLayoutModule", menuName = "Story/Modules/Stage Layout")]
    public sealed class StoryStageLayoutModuleSO : StoryModuleSO
    {
        [Tooltip("이 라인 시점의 스테이지 위 캐릭터 목록. 없는 캐릭터는 퇴장 처리됨.")]
        [SerializeField] private List<StoryActorStateData> actors = new();

        public IReadOnlyList<StoryActorStateData> Actors => actors;

        public override string DisplayName => "Stage Layout";

#if UNITY_EDITOR
        /// <summary>에디터 프리뷰/Inspector에서 직접 수정하는 접근자.</summary>
        public List<StoryActorStateData> ActorsEditable => actors;
#endif
    }
}
