using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// StoryChoiceModuleSO 를 graph에서 시각화하는 미니 노드.
    /// 부모 StoryGraphNodeView 아래에 절대 위치로 배치된다.
    /// </summary>
    public sealed class StoryChoiceNodeView : VisualElement
    {
        // ── 레이아웃 상수 (ReactionPortPos 계산 공유) ─
        public const float NodeW       = 230f;
        public const float HeaderH     = 24f;
        public const float BodyPadTop  = 4f;
        public const float RowH        = 20f;
        private const float PortR      = 7f;
        private const float PortD      = PortR * 2f;

        // ── 이벤트 ───────────────────────────────────
        /// <summary>option 포트 PointerDown → 드래그 연결 시작.</summary>
        public event Action<StoryChoiceNodeView, int> ReactionPortDragStart;
        /// <summary>disconnect 등 데이터 변경 후 캔버스 리페인트 요청.</summary>
        public event Action Changed;

        // ── 데이터 ──────────────────────────────────
        public StoryLineSO         Line   { get; }
        public StoryChoiceModuleSO Choice { get; }

        public float ZoomScale { get; set; } = 1f;

        // ── 내부 참조 ────────────────────────────────
        private readonly Label              _titleLabel;
        private readonly List<Label>        _reactionLabels = new();

        // ── 생성자 ───────────────────────────────────

        public StoryChoiceNodeView(StoryLineSO line, StoryChoiceModuleSO choice)
        {
            Line   = line;
            Choice = choice;

            style.position = Position.Absolute;
            style.width    = NodeW;
            style.overflow = Overflow.Hidden;
            style.borderTopLeftRadius    = style.borderTopRightRadius    =
            style.borderBottomLeftRadius = style.borderBottomRightRadius = 4;
            style.backgroundColor = new StyleColor(new Color(0.20f, 0.16f, 0.10f));
            ApplyBorder(new Color(0.72f, 0.52f, 0.18f), 1.5f);

            // ── Header ─────────────────────────────────
            var header = new VisualElement();
            header.style.backgroundColor      = new StyleColor(new Color(0.42f, 0.28f, 0.06f));
            header.style.flexDirection        = FlexDirection.Row;
            header.style.alignItems           = Align.Center;
            header.style.paddingLeft          = 6;
            header.style.paddingRight         = 6;
            header.style.paddingTop           = 3;
            header.style.paddingBottom        = 3;
            header.style.borderTopLeftRadius  = 3;
            header.style.borderTopRightRadius = 3;

            var diamond = new Label("⬦");
            diamond.style.fontSize    = 11;
            diamond.style.marginRight = 4;
            diamond.style.color       = new StyleColor(new Color(1f, 0.78f, 0.3f));
            header.Add(diamond);

            string titleText = string.IsNullOrEmpty(choice?.ChoiceId) ? "CHOICE" : choice.ChoiceId;
            _titleLabel = new Label(titleText);
            _titleLabel.style.fontSize                = 10;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.flexGrow                = 1;
            _titleLabel.style.overflow                = Overflow.Hidden;
            header.Add(_titleLabel);
            Add(header);

            // ── Body: option rows ─────────────────────
            var body = new VisualElement();
            body.style.paddingLeft   = 6;
            body.style.paddingRight  = 2;
            body.style.paddingTop    = (StyleLength)BodyPadTop;
            body.style.paddingBottom = 4;

            if (choice != null)
            {
                for (int i = 0; i < choice.Options.Count; i++)
                    body.Add(BuildOptionRow(i, choice.Options[i]));
            }
            Add(body);
        }

        // ── 공개 API ─────────────────────────────────

        public void Refresh()
        {
            if (Choice == null) return;
            _titleLabel.text = string.IsNullOrEmpty(Choice.ChoiceId) ? "CHOICE" : Choice.ChoiceId;
            for (int i = 0; i < Choice.Options.Count && i < _reactionLabels.Count; i++)
            {
                var opt = Choice.Options[i];
                _reactionLabels[i].text = string.IsNullOrEmpty(opt.reactionStartLineId)
                    ? "—" : Trunc(opt.reactionStartLineId, 12);
            }
        }

        /// <summary>
        /// canvas 로컬 좌표 기준 option i 포트의 우측 중앙 위치.
        /// DrawConnections 에서 베지어 시작점으로 사용한다.
        /// </summary>
        public Vector2 ReactionPortPos(int index) => new Vector2(
            layout.xMax - PortR,
            layout.yMin + HeaderH + BodyPadTop + index * RowH + RowH * 0.5f);

        // ── Option 행 ────────────────────────────────

        private VisualElement BuildOptionRow(int index, StoryChoiceModuleSO.ChoiceOption opt)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.height        = (StyleLength)RowH;
            row.style.overflow      = Overflow.Hidden;

            // 번호 배지
            var idx = new Label($"{index}.");
            idx.style.fontSize    = 9;
            idx.style.color       = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            idx.style.minWidth    = 14;
            idx.style.marginRight = 2;
            row.Add(idx);

            // option 텍스트
            var textLbl = new Label(Trunc(opt.text, 12));
            textLbl.style.fontSize = 9;
            textLbl.style.flexGrow = 1;
            textLbl.style.overflow = Overflow.Hidden;
            textLbl.style.color    = new StyleColor(new Color(0.87f, 0.87f, 0.87f));
            row.Add(textLbl);

            // reaction 타겟 레이블
            var reactionLbl = new Label(string.IsNullOrEmpty(opt.reactionStartLineId)
                ? "—" : Trunc(opt.reactionStartLineId, 10));
            reactionLbl.style.fontSize    = 9;
            reactionLbl.style.color       = new StyleColor(new Color(0.4f, 0.8f, 0.45f));
            reactionLbl.style.marginRight = 3;
            reactionLbl.style.overflow    = Overflow.Hidden;
            row.Add(reactionLbl);
            _reactionLabels.Add(reactionLbl);

            // 출력 포트
            var port = MakePort(new Color(0.3f, 0.7f, 0.35f));
            int capturedIdx = index;
            port.tooltip = "드래그 → reaction 연결  /  우클릭 → Disconnect";
            port.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                ReactionPortDragStart?.Invoke(this, capturedIdx);
                e.StopPropagation();
            });
            port.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                bool has = !string.IsNullOrEmpty(Choice.Options[capturedIdx].reactionStartLineId);
                evt.menu.AppendAction("Disconnect",
                    _ => DisconnectReaction(capturedIdx),
                    has ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }));
            row.Add(port);

            return row;
        }

        // ── Disconnect ───────────────────────────────

        private void DisconnectReaction(int optIdx)
        {
            Undo.RecordObject(Choice, "Disconnect Choice Reaction");
            var so = new SerializedObject(Choice);
            so.Update();
            so.FindProperty("options")
              .GetArrayElementAtIndex(optIdx)
              .FindPropertyRelative("reactionStartLineId")
              .stringValue = "";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(Choice);
            Refresh();
            Changed?.Invoke();
        }

        // ── 헬퍼 ─────────────────────────────────────

        private void ApplyBorder(Color c, float w)
        {
            var sc = new StyleColor(c);
            style.borderTopColor    = sc; style.borderRightColor   = sc;
            style.borderBottomColor = sc; style.borderLeftColor    = sc;
            style.borderTopWidth    = w;  style.borderRightWidth   = w;
            style.borderBottomWidth = w;  style.borderLeftWidth    = w;
        }

        private static VisualElement MakePort(Color col)
        {
            var p = new VisualElement();
            p.style.width           = PortD;
            p.style.height          = PortD;
            p.style.backgroundColor = new StyleColor(col);
            p.style.flexShrink      = 0;
            p.style.borderTopLeftRadius    = p.style.borderTopRightRadius    =
            p.style.borderBottomLeftRadius = p.style.borderBottomRightRadius = PortR;
            return p;
        }

        private static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace('\n', ' ');
            return s.Length <= max ? s : s[..max] + "…";
        }
    }
}
