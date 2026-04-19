using System;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules
{
    /// <summary>
    /// 한 라인 시점에서 특정 캐릭터의 스테이지 상태 스냅샷.
    /// StoryStageLayoutModuleSO 의 actors 리스트 원소로 직렬화된다.
    /// 에디터 프리뷰에서 드래그로 조정하고 저장하면 런타임에도 그대로 반영된다.
    /// </summary>
    [Serializable]
    public sealed class StoryActorStateData
    {
        [Tooltip("배치할 캐릭터")]
        public _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.CharacterDefinitionSO actor;

        [Tooltip("스테이지 내 정규화 위치. X:0=왼쪽 1=오른쪽 / Y:0=아래(지면) 1=위")]
        public Vector2 normalizedPosition = new Vector2(0.5f, 0f);

        [Tooltip("X 스케일 (음수 = 좌우 반전). 크기 보정에도 사용.")]
        public float scaleX = 1f;

        [Tooltip("이 라인에서 캐릭터가 보이는지 여부")]
        public bool visible = true;

        [Tooltip("포커스 상태. false = 비포커스(어두운) 연출")]
        public bool focused = true;

        [Tooltip("스테이지 정렬 우선순위 (높을수록 앞에 그려짐)")]
        public int sortOrder = 0;

        [Tooltip("이 라인에서 처음 등장할 때 사용하는 연출 방식")]
        public StoryEnterMotionType enterMotion = StoryEnterMotionType.FadeIn;

        [Tooltip("등장 연출 시간(초). Instant 일 때는 무시됨.")]
        [Range(0f, 2f)]
        public float enterDuration = 0.35f;

        [Tooltip("이전 라인 대비 위치가 달라질 때 이동 연출 시간(초)")]
        [Range(0f, 2f)]
        public float moveDuration = 0.2f;

        [Tooltip("포즈/표정 키 (향후 애니메이션 시스템 확장용)")]
        public string poseKey = "";

        public StoryActorStateData ShallowClone() => (StoryActorStateData)MemberwiseClone();
    }
}
