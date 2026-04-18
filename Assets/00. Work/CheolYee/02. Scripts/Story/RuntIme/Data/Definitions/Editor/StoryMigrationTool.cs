using System;
using System.Collections.Generic;
using System.Text;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using UnityEditor;
using UnityEngine;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// StoryLineSO.inlineModules 항목을 StoryModuleSO 서브에셋으로 변환하는 에디터 도구.
    ///
    /// ■ 메뉴
    ///   Tools/Story/[Preview] Inline → SO Migration  — 실제 변경 없이 결과만 로그
    ///   Tools/Story/Migrate Inline Modules → SO      — 확인 다이얼로그 후 실제 변환
    ///
    /// ■ 동작
    ///   1. Assets/00. Work/CheolYee 내 모든 StoryLineSO를 탐색
    ///   2. inlineModules 가 있는 라인만 처리
    ///   3. 각 인라인 모듈에 대응하는 StoryModuleSO 서브에셋을 생성, 필드 복사
    ///   4. line.modules 리스트에 추가
    ///   5. 변환 완료된 inlineModules 항목을 리스트에서 제거 (필드 선언은 유지)
    ///
    /// ■ 멱등성
    ///   서브에셋 이름에 "[Migrated]" 접두사를 붙여 중복 실행을 감지·건너뜀.
    ///
    /// ■ 주의
    ///   AssetDatabase 작업은 Undo 불가. Preview로 결과를 반드시 먼저 확인하세요.
    /// </summary>
    public static class StoryMigrationTool
    {
        private const string SearchRoot  = "Assets/00. Work/CheolYee";
        private const string MigratedTag = "[Migrated]";

        // ── 메뉴 ──────────────────────────────────────

        [MenuItem("Tools/Story/[Preview] Inline → SO Migration")]
        private static void MenuPreview() => RunPipeline(dryRun: true);

        [MenuItem("Tools/Story/Migrate Inline Modules → SO Sub-assets")]
        private static void MenuMigrate()
        {
            // 먼저 dry-run 으로 변환 대상 파악
            var previewSb = new StringBuilder();
            int total     = CollectAll(previewSb, dryRun: true);

            if (total == 0)
            {
                EditorUtility.DisplayDialog("Migration",
                    "변환할 인라인 모듈이 없습니다.\n(이미 모두 변환되었거나 대상 없음)", "확인");
                return;
            }

            bool ok = EditorUtility.DisplayDialog(
                "Inline → SO Migration",
                $"총 {total}개 인라인 모듈을 SO 서브에셋으로 변환합니다.\n\n" +
                $"{previewSb}\n⚠ 이 작업은 Undo되지 않습니다. 진행하시겠습니까?",
                "변환 실행", "취소");

            if (ok) RunPipeline(dryRun: false);
        }

        // ── 실행 ──────────────────────────────────────

        private static void RunPipeline(bool dryRun)
        {
            var sb    = new StringBuilder();
            int total = CollectAll(sb, dryRun);

            string tag = dryRun ? "[Preview]" : "[Migration]";
            Debug.Log($"<b>[StoryMigration]{tag}</b> 총 {total}개 모듈 처리.\n{sb}");

            if (!dryRun)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Migration 완료",
                    $"{total}개 모듈 변환 완료.\nConsole 로그를 확인하세요.", "확인");
            }
        }

        // ── 전체 탐색 루프 ────────────────────────────

        private static int CollectAll(StringBuilder sb, bool dryRun)
        {
            string[] guids = AssetDatabase.FindAssets("t:StoryLineSO", new[] { SearchRoot });
            int total = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var    line = AssetDatabase.LoadAssetAtPath<StoryLineSO>(path);

                if (line == null
                    || line.InlineModules == null
                    || line.InlineModules.Count == 0)
                    continue;

                total += ProcessLine(line, path, dryRun, sb);
            }

            return total;
        }

        // ── 라인 단위 처리 ────────────────────────────

        private static int ProcessLine(
            StoryLineSO line,
            string       path,
            bool         dryRun,
            StringBuilder sb)
        {
            // 이미 변환된 서브에셋 이름 집합 (멱등성 보장)
            var existingNames = new HashSet<string>();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj != null && obj.name.StartsWith(MigratedTag))
                    existingNames.Add(obj.name);

            var soLine      = new SerializedObject(line);
            soLine.Update();
            var inlineProp  = soLine.FindProperty("inlineModules");
            var modulesProp = soLine.FindProperty("modules");

            int       count          = 0;
            var       removeIndices  = new List<int>();   // 처리 완료 인덱스 (역순 삭제)

            for (int i = 0; i < inlineProp.arraySize; i++)
            {
                var elem = inlineProp.GetArrayElementAtIndex(i);
                if (elem.managedReferenceValue is not StoryInlineModuleData data) continue;

                string subName = BuildSubAssetName(data, i);

                // 이미 변환 완료 → inline 정리만 예약
                if (existingNames.Contains(subName))
                {
                    sb.AppendLine($"  ↷ [{line.LineId}] #{i} {data.GetType().Name} — 기존 서브에셋 확인, inline 항목 제거 예약");
                    removeIndices.Add(i);
                    count++;
                    continue;
                }

                // 대응 SO 타입 결정
                StoryModuleSO newSO = InstantiateSO(data.GetType());
                if (newSO == null)
                {
                    sb.AppendLine($"  ⚠ [{line.LineId}] #{i} {data.GetType().Name} — 대응 SO 타입 없음, 건너뜀");
                    continue;
                }

                newSO.name = subName;
                count++;

                if (dryRun)
                {
                    sb.AppendLine($"  ✦ [{line.LineId}] #{i} {data.GetType().Name} → {newSO.GetType().Name}");
                    UnityEngine.Object.DestroyImmediate(newSO);
                    continue;
                }

                // 필드 복사
                var soNew = new SerializedObject(newSO);
                CopyBase(elem, soNew);
                CopySpecific(data.GetType(), elem, soNew);
                soNew.ApplyModifiedPropertiesWithoutUndo();

                // 서브에셋 등록 (StoryLineSO 파일 내부에 포함)
                AssetDatabase.AddObjectToAsset(newSO, line);

                // modules 리스트 끝에 추가
                modulesProp.arraySize++;
                modulesProp.GetArrayElementAtIndex(modulesProp.arraySize - 1)
                           .objectReferenceValue = newSO;

                removeIndices.Add(i);
                sb.AppendLine($"  ✔ [{line.LineId}] #{i} {data.GetType().Name} → {newSO.GetType().Name}  \"{subName}\"");
            }

            // inline 항목 역순 삭제 (필드 선언은 유지, 내용만 정리)
            if (!dryRun && removeIndices.Count > 0)
            {
                removeIndices.Sort((a, b) => b - a);
                foreach (int idx in removeIndices)
                    if (idx < inlineProp.arraySize)
                        inlineProp.DeleteArrayElementAtIndex(idx);

                soLine.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(line);
            }

            return count;
        }

        // ── SO 인스턴스 생성 ──────────────────────────

        private static string BuildSubAssetName(StoryInlineModuleData data, int idx)
            => $"{MigratedTag} {data.DisplayName} #{idx}";

        private static StoryModuleSO InstantiateSO(Type t)
        {
            if (t == typeof(WaitInlineModuleData))
                return ScriptableObject.CreateInstance<StoryWaitModuleSO>();
            if (t == typeof(CharacterClearInlineModuleData))
                return ScriptableObject.CreateInstance<StoryCharacterClearModuleSO>();
            if (t == typeof(ChoiceInlineModuleData))
                return ScriptableObject.CreateInstance<StoryChoiceModuleSO>();
            return null;
        }

        // ── 필드 복사 ────────────────────────────────

        private static void CopyBase(SerializedProperty src, SerializedObject dst)
        {
            CopyScalar(src.FindPropertyRelative("timing"),             dst.FindProperty("timing"));
            CopyScalar(src.FindPropertyRelative("isBlocking"),         dst.FindProperty("isBlocking"));
            CopyScalar(src.FindPropertyRelative("canSkip"),            dst.FindProperty("canSkip"));
            CopyScalar(src.FindPropertyRelative("affectsAutoAdvance"), dst.FindProperty("affectsAutoAdvance"));
        }

        private static void CopySpecific(Type t, SerializedProperty src, SerializedObject dst)
        {
            if (t == typeof(WaitInlineModuleData))
            {
                CopyScalar(src.FindPropertyRelative("duration"),        dst.FindProperty("duration"));
                CopyScalar(src.FindPropertyRelative("useUnscaledTime"), dst.FindProperty("useUnscaledTime"));
                return;
            }

            if (t == typeof(ChoiceInlineModuleData))
            {
                CopyScalar(src.FindPropertyRelative("choiceId"), dst.FindProperty("choiceId"));
                CopyOptionsArray(
                    src.FindPropertyRelative("options"),
                    dst.FindProperty("options"));
            }
            // CharacterClear: 추가 필드 없음
        }

        private static void CopyOptionsArray(SerializedProperty srcArr, SerializedProperty dstArr)
        {
            if (srcArr == null || dstArr == null) return;
            dstArr.arraySize = srcArr.arraySize;
            for (int i = 0; i < srcArr.arraySize; i++)
            {
                var s = srcArr.GetArrayElementAtIndex(i);
                var d = dstArr.GetArrayElementAtIndex(i);
                CopyScalar(s.FindPropertyRelative("optionId"),            d.FindPropertyRelative("optionId"));
                CopyScalar(s.FindPropertyRelative("text"),                d.FindPropertyRelative("text"));
                CopyScalar(s.FindPropertyRelative("reactionStartLineId"), d.FindPropertyRelative("reactionStartLineId"));
                CopyScalar(s.FindPropertyRelative("convergeLineId"),      d.FindPropertyRelative("convergeLineId"));
            }
        }

        private static void CopyScalar(SerializedProperty src, SerializedProperty dst)
        {
            if (src == null || dst == null) return;
            switch (src.propertyType)
            {
                case SerializedPropertyType.Boolean: dst.boolValue       = src.boolValue;       break;
                case SerializedPropertyType.Float:   dst.floatValue      = src.floatValue;      break;
                case SerializedPropertyType.String:  dst.stringValue     = src.stringValue;     break;
                case SerializedPropertyType.Enum:    dst.enumValueIndex  = src.enumValueIndex;  break;
                case SerializedPropertyType.Integer: dst.intValue        = src.intValue;        break;
                case SerializedPropertyType.ObjectReference:
                    dst.objectReferenceValue = src.objectReferenceValue;                        break;
            }
        }
    }
}
