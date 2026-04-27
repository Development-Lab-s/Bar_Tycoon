using System.Collections.Generic;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    /// <summary>
    /// 그래프 에디터에서 출력 포트 연결이 필요한 모듈의 capability 인터페이스.
    /// 이 인터페이스를 구현한 모듈만 포트 스트립 UI를 받습니다. (Phase 3 에디터 통일 시 활용)
    /// SetPortConnection 은 에디터에서 반드시 SerializedObject 를 통해 호출해야 Undo가 정상 동작합니다.
    /// </summary>
    public interface IStoryGraphConnectableModule
    {
        IReadOnlyList<StoryModulePortDescriptor> GetPorts();
        string GetPortConnection(int portIndex);
        void   SetPortConnection(int portIndex, string targetLineId);
    }

    public readonly struct StoryModulePortDescriptor
    {
        public int    Index { get; }
        public string Label { get; }

        public StoryModulePortDescriptor(int index, string label)
        {
            Index = index;
            Label = label;
        }
    }
}
