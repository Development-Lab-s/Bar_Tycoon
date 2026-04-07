using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.RuntimeModules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Presentation.Directors
{
    public sealed class BasicStoryLogController : MonoBehaviour, IStoryLogController
    {
        private readonly List<StoryLogEntry> _entries = new();

        public IReadOnlyList<StoryLogEntry> Entries => _entries;

        public void AppendLine(StoryLogEntry entry)
        {
            if (entry == null)
                return;

            _entries.Add(entry);
        }

        public void AppendChoiceResult(StoryLogEntry entry)
        {
            if (entry == null)
                return;

            _entries.Add(entry);
        }

        public void Open()
        {
            // TODO:
            // 실제 로그 UI 패널 열기
        }

        public void Close()
        {
            // TODO:
            // 실제 로그 UI 패널 닫기
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}