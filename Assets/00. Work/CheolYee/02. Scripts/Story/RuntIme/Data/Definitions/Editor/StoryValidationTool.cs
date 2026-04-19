using System.Collections.Generic;
using System.Text;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 스토리 에셋의 정합성을 검사하는 에디터 도구.
    ///
    /// 메뉴: Tools/Story/Validate Story Assets
    ///
    /// ■ 검사 항목
    ///   [ERROR]
    ///     - episode.lines[] null 항목
    ///     - 빈 LineId
    ///     - 중복 LineId (동일 에피소드 내)
    ///     - nextLineId 자기 참조 (무한 루프)
    ///     - nextLineId / reactionStartLineId 가 에피소드에 없음
    ///     - entryLineId 가 lines 에 없음
    ///
    ///   [WARN]
    ///     - inlineModules + 동명 [Migrated] SO 공존 (이중 실행 위험)
    ///     - inlineModules 내 null 항목
    ///     - 고아 서브에셋 (modules 리스트에 없는 StoryModuleSO 서브에셋)
    ///
    ///   [INFO]
    ///     - 에피소드에 속하지 않는 StoryLineSO (고아 라인)
    ///     - 미변환 인라인 모듈이 있는 라인 (마이그레이션 도구 실행 권장)
    /// </summary>
    public static class StoryValidationTool
    {
        private const string SearchRoot = "Assets/00. Work/CheolYee";

        [MenuItem("Tools/Story/Validate Story Assets")]
        public static void ValidateAll()
        {
            var report = new ValidationReport();

            string[] epGuids = AssetDatabase.FindAssets("t:StoryEpisodeSO", new[] { SearchRoot });
            if (epGuids.Length == 0)
                report.Add(Severity.Warn, "global", "StoryEpisodeSO 를 찾을 수 없습니다.");

            var allEpLineGuids = new HashSet<string>();   // 에피소드에 포함된 StoryLineSO GUID

            foreach (string guid in epGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var    ep   = AssetDatabase.LoadAssetAtPath<StoryEpisodeSO>(path);
                if (ep == null) continue;
                ValidateEpisode(ep, report, allEpLineGuids);
            }

            // 어느 에피소드에도 속하지 않는 고아 StoryLineSO
            string[] lineGuids = AssetDatabase.FindAssets("t:StoryLineSO", new[] { SearchRoot });
            foreach (string g in lineGuids)
            {
                if (allEpLineGuids.Contains(g)) continue;
                string p = AssetDatabase.GUIDToAssetPath(g);
                var    l = AssetDatabase.LoadAssetAtPath<StoryLineSO>(p);
                if (l != null)
                    report.Add(Severity.Info, "global",
                        $"에피소드에 속하지 않는 StoryLineSO: lineId=\"{l.LineId}\"  path={p}");
            }

            report.Print();
        }

        // ── 에피소드 검사 ─────────────────────────────

        private static void ValidateEpisode(
            StoryEpisodeSO ep,
            ValidationReport report,
            HashSet<string> allEpLineGuids)
        {
            string ctx = $"[EP:{ep.EpisodeId ?? ep.name}]";

            var lineIdSet = new HashSet<string>();
            var idCount   = new Dictionary<string, int>();

            for (int i = 0; i < ep.Lines.Count; i++)
            {
                var line = ep.Lines[i];
                if (line == null)
                {
                    report.Add(Severity.Error, ctx, $"lines[{i}] = null");
                    continue;
                }

                string lineGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(line));
                if (!string.IsNullOrEmpty(lineGuid))
                    allEpLineGuids.Add(lineGuid);

                if (string.IsNullOrWhiteSpace(line.LineId))
                {
                    report.Add(Severity.Error, ctx, $"lines[{i}] ({line.name}) LineId 가 비어 있음");
                    continue;
                }

                // 앞뒤 공백이 있으면 경고 (비교는 Trim 기준으로 수행)
                string trimmedId = line.LineId.Trim();
                if (trimmedId != line.LineId)
                    report.Add(Severity.Warn, ctx,
                        $"lines[{i}] LineId \"{line.LineId}\" 에 앞뒤 공백이 있음 — Inspector에서 수정하세요");

                lineIdSet.Add(trimmedId);

                idCount.TryGetValue(trimmedId, out int n);
                idCount[trimmedId] = n + 1;
            }

            // 중복 LineId
            foreach (var kv in idCount)
                if (kv.Value > 1)
                    report.Add(Severity.Error, ctx, $"중복 LineId \"{kv.Key}\" ({kv.Value}개)");

            // entryLineId (Trim 기준 비교)
            string trimmedEntry = ep.EntryLineId?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(ep.EntryLineId))
            {
                if (trimmedEntry != ep.EntryLineId)
                    report.Add(Severity.Warn, ctx,
                        $"entryLineId \"{ep.EntryLineId}\" 에 앞뒤 공백이 있음 — Inspector에서 수정하세요");

                if (!lineIdSet.Contains(trimmedEntry))
                    report.Add(Severity.Error, ctx,
                        $"entryLineId \"{ep.EntryLineId}\" 가 라인 목록에 없음");
            }

            // 각 라인 상세 검사
            foreach (var line in ep.Lines)
            {
                if (line == null) continue;
                ValidateLine(line, lineIdSet, ctx, report);
            }
        }

        // ── 라인 검사 ────────────────────────────────

        private static void ValidateLine(
            StoryLineSO      line,
            HashSet<string>  epLineIds,
            string           epCtx,
            ValidationReport report)
        {
            string ctx = $"{epCtx} line:{line.LineId}";

            // nextLineId
            if (!string.IsNullOrWhiteSpace(line.NextLineId))
            {
                string nextTrimmed = line.NextLineId.Trim();
                string selfTrimmed = line.LineId?.Trim() ?? "";
                if (nextTrimmed != line.NextLineId)
                    report.Add(Severity.Warn, ctx,
                        $"nextLineId \"{line.NextLineId}\" 에 앞뒤 공백이 있음 — Inspector에서 수정하세요");

                if (nextTrimmed == selfTrimmed)
                    report.Add(Severity.Error, ctx, "nextLineId 가 자기 자신 참조 (무한 루프)");
                else if (!epLineIds.Contains(nextTrimmed))
                    report.Add(Severity.Error, ctx,
                        $"nextLineId \"{line.NextLineId}\" 가 에피소드에 없음");
            }

            // SO modules
            for (int i = 0; i < line.Modules.Count; i++)
            {
                var m = line.Modules[i];
                if (m == null)
                {
                    report.Add(Severity.Error, ctx, $"modules[{i}] = null");
                    continue;
                }
                if (m is IStoryChoiceLikeModule cl)
                    ValidateChoiceOptions(cl, epLineIds, ctx, "SO", report);
            }

            CheckOrphanedSubAssets(line, ctx, report);
        }

        // ── Choice option 참조 검사 ────────────────────

        private static void ValidateChoiceOptions(
            IStoryChoiceLikeModule module,
            HashSet<string>        epLineIds,
            string                 ctx,
            string                 source,
            ValidationReport       report)
        {
            if (module.Options == null) return;
            for (int i = 0; i < module.Options.Count; i++)
            {
                var opt = module.Options[i];
                if (opt == null) continue;

                string reactionId = opt.ReactionStartLineId;
                if (string.IsNullOrWhiteSpace(reactionId)) continue;

                string trimmedReaction = reactionId.Trim();
                if (trimmedReaction != reactionId)
                    report.Add(Severity.Warn, ctx,
                        $"[{source}] option[{i}].reactionStartLineId \"{reactionId}\" 에 앞뒤 공백이 있음");

                if (!epLineIds.Contains(trimmedReaction))
                    report.Add(Severity.Error, ctx,
                        $"[{source}] option[{i}].reactionStartLineId \"{reactionId}\" 가 에피소드에 없음");
            }
        }

        // ── 고아 서브에셋 검사 ─────────────────────────

        private static void CheckOrphanedSubAssets(
            StoryLineSO      line,
            string           ctx,
            ValidationReport report)
        {
            string path = AssetDatabase.GetAssetPath(line);
            if (string.IsNullOrEmpty(path)) return;

            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj == null || obj == line) continue;
                if (obj is not StoryModuleSO sub) continue;

                bool referenced = false;
                foreach (var m in line.Modules)
                    if (m == sub) { referenced = true; break; }

                if (!referenced)
                    report.Add(Severity.Warn, ctx,
                        $"고아 서브에셋: \"{sub.name}\" ({sub.GetType().Name}) — modules 리스트에 없음");
            }
        }


        // ── ValidationReport ──────────────────────────

        private enum Severity { Info, Warn, Error }

        private sealed class ValidationReport
        {
            private readonly List<(Severity sev, string ctx, string msg)> _items = new();
            private int _errors, _warns, _infos;

            public void Add(Severity sev, string ctx, string msg)
            {
                _items.Add((sev, ctx, msg));
                switch (sev)
                {
                    case Severity.Error: _errors++; break;
                    case Severity.Warn:  _warns++;  break;
                    case Severity.Info:  _infos++;  break;
                }
            }

            public void Print()
            {
                var sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════");
                sb.AppendLine("  Story Asset Validation Report");
                sb.AppendLine("═══════════════════════════════════════");

                foreach (var (sev, ctx, msg) in _items)
                {
                    switch (sev)
                    {
                        case Severity.Error:
                            sb.AppendLine($"  <color=red>[ERROR]</color>  {ctx}: {msg}");
                            break;
                        case Severity.Warn:
                            sb.AppendLine($"  <color=yellow>[WARN]</color>   {ctx}: {msg}");
                            break;
                        case Severity.Info:
                            sb.AppendLine($"  <color=grey>[INFO]</color>   {ctx}: {msg}");
                            break;
                    }
                }

                sb.AppendLine("───────────────────────────────────────");
                sb.AppendLine($"  요약: {_errors} ERROR  |  {_warns} WARN  |  {_infos} INFO");
                sb.AppendLine("═══════════════════════════════════════");

                if (_errors > 0)       Debug.LogError(sb.ToString());
                else if (_warns  > 0)  Debug.LogWarning(sb.ToString());
                else                   Debug.Log(sb.ToString());
            }
        }
    }
}
