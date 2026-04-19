using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions
{
    /// <summary>
    /// 스토리 시스템에서 사용되는 캐릭터의 정의를 담는 ScriptableObject 클래스입니다.
    /// 캐릭터의 ID, 이름, 아이콘, 기본 프리팹 등의 정보를 포함합니다.
    /// </summary>
    
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Story/Character Definition")]
    public sealed class CharacterDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string characterId; 
        //스토리 시스템 내에서 캐릭터를 고유하게 식별하는 ID입니다.
        //대사나 이벤트에서 이 ID를 참조하여 캐릭터를 지정합니다.
        [SerializeField] private string displayName;
        //스토리 진행 중 캐릭터의 이름을 표시할 때 사용하는 문자열입니다.

        [Header("Presentation")]
        [SerializeField] private Sprite logIcon;
        //스토리 로그나 UI에서 캐릭터를 시각적으로 나타낼 때 사용하는 아이콘입니다.
        [SerializeField] private GameObject defaultActorPrefab;
        //스토리 진행 중 캐릭터가 화면에 등장할 때 사용하는 기본 프리팹입니다.
        //이 프리팹은 캐릭터의 시각적 표현과 애니이션을 담당할 수 있습니다.

        [Header("Preview")]
        [Tooltip("StoryPreviewWindow 에서 표시할 스프라이트. 없으면 LogIcon 사용.")]
        [SerializeField] private Sprite previewSprite;

        //캐릭터 정의의 각 필드에 대한 공개 읽기 전용 프로퍼티입니다.
        public string CharacterId => characterId;
        public string DisplayName => displayName;
        public Sprite LogIcon => logIcon;
        public GameObject DefaultActorPrefab => defaultActorPrefab;
        public Sprite PreviewSprite => previewSprite != null ? previewSprite : logIcon;
    }
}