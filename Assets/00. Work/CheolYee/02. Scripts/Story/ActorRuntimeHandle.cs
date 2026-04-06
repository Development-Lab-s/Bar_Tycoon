using System;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story
{
    /// <summary>
    /// 스토리 시스템에서 사용되는 배우의 런타임 핸들을 나타내는 클래스입니다.
    /// 각 핸들은 배우 인스턴스 ID, 캐릭터 정의, 현재 앵커 위치, 로컬 오프셋, 정렬 순서,
    /// 그리고 포커스 여부 등의 정보를 포함합니다.
    /// 이 핸들은 스토리 진행 중 배우의 상태와 위치를 관리하는 데 사용됩니다.
    /// </summary>
    
    [Serializable]
    public sealed class ActorRuntimeHandle
    {
        // 배우 인스턴스 ID입니다. 스토리 진행 중 각 배우 인스턴스를 고유하게 식별하는 문자열입니다.
        public string actorInstanceId;
        // 이 핸들이 참조하는 캐릭터 정의입니다. 배우가 어떤 캐릭터를 나타내는지 식별하는 데 사용됩니다.
        public CharacterDefinitionSO character;
        // 현재 앵커 위치입니다. 배우가 화면의 어느 위치에 배치되어 있는지를 나타내는 열거형입니다.
        public StageAnchorType currentAnchor;
        // 배우의 로컬 오프셋입니다.
        // 앵커 위치를 기준으로 배우가 화면에서 얼마나 떨어져 있는지를 나타내는 2D 벡터입니다.
        public Vector2 localOffset;
        // 배우의 정렬 순서입니다. 여러 배우가 겹쳐질 때, 이 값이 낮을수록 뒤에 배치되고, 높을수록 앞에 배치됩니다.
        public int sortOrder;
        // 이 배우가 현재 포커스 상태인지 여부를 나타내는 불리언입니다.
        // 포커스 상태인 배우는 대사 진행 중에 강조되어 표시될 수 있습니다.
        public bool isFocused;
    }
}