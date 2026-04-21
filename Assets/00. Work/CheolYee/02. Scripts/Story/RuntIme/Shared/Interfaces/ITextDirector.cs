using System.Threading;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using Cysharp.Threading.Tasks;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces
{
    /// 대사 디렉터 인터페이스입니다. 대사 진행과 관련된 모든 기능을 정의합니다.
    public interface ITextDirector
    {
        bool IsTyping { get; }
        UniTask PlayLineAsync(StoryLineSO line, CancellationToken ct);
        void CompleteCurrentLine();
        void Clear();
    }
}