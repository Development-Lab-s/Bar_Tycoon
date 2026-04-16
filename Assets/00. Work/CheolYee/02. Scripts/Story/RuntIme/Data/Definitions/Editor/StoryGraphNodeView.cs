using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    public sealed class StoryGraphNodeView : VisualElement
    {
        // ── 기본 크기 상수 ───────────────────────────
        public const float DefaultW      = 260f;
        public const float DefaultH      = 120f;
        public const float MinW          = 220f;
        public const float MinH          = 100f;
        private const float PortRadius   = 7f;
        private const float PortDiameter = PortRadius * 2f;
        private const float GripSize     = 14f;

        // ── 공개 이벤트 ──────────────────────────────
        public event Action<StoryGraphNodeView>          Clicked;
        public event Action<StoryGraphNodeView>          DoubleClicked;
        public event Action<StoryGraphNodeView>          OutputPortClicked;
        /// <summary>드래그 이동 종료 → (node, finalPos)</summary>
        public event Action<StoryGraphNodeView, Vector2> Dragged;
        /// <summary>리사이즈 종료 → (node, finalSize)</summary>
        public event Action<StoryGraphNodeView, Vector2> Resized;
        /// <summary>출력 포트 우클릭 Disconnect 선택</summary>
        public event Action<StoryGraphNodeView>          DisconnectRequested;

        // ── 외부 주입 ────────────────────────────────
        /// <summary>연결 모드 중이면 드래그/리사이즈 금지. Canvas가 주입.</summary>
        public Func<bool> IsConnectModeActive;

        // ── 데이터 ──────────────────────────────────
        public StoryLineSO Line { get; }

        // ── 시각 요소 참조 ───────────────────────────
        private readonly Label         _idLabel;
        private readonly VisualElement _inputPort;
        private readonly VisualElement _outputPort;
        private readonly VisualElement _resizeHandle;

        // ── 현재 크기 ────────────────────────────────
        private float _w;
        private float _h;

        // ── 드래그(이동) 상태 ────────────────────────
        private bool    _dragging;
        private bool    _dragMoved;
        private Vector2 _dragStartScreen;
        private Vector2 _nodeStartPos;
        private const float DragThreshold = 5f;

        // ── 리사이즈 상태 ────────────────────────────
        private bool    _resizing;
        private Vector2 _resizeStartScreen;
        private Vector2 _sizeAtResizeStart;

        // ── 생성자 ───────────────────────────────────

        public StoryGraphNodeView(StoryLineSO line, float w = DefaultW, float h = DefaultH)
        {
            Line = line;

            style.position = Position.Absolute;
            style.overflow = Overflow.Hidden;
            style.borderTopLeftRadius = style.borderTopRightRadius =
            style.borderBottomLeftRadius = style.borderBottomRightRadius = 4;
            style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            ApplyBorder(new Color(0.35f, 0.35f, 0.35f), 1f);

            // ── Header ────────────────────────────────
            var header = new VisualElement();
            header.style.backgroundColor      = new StyleColor(new Color(0.14f, 0.27f, 0.44f));
            header.style.paddingLeft          = 8;
            header.style.paddingTop           = 4;
            header.style.paddingBottom        = 4;
            header.style.borderTopLeftRadius  = 3;
            header.style.borderTopRightRadius = 3;
            header.style.flexShrink           = 0;

            _idLabel = new Label(IdText(line));
            _idLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _idLabel.style.fontSize = 11;
            _idLabel.style.overflow = Overflow.Hidden;
            header.Add(_idLabel);
            Add(header);

            // ── Body ─────────────────────────────────
            var body = new VisualElement();
            body.style.paddingLeft  = 8;
            body.style.paddingRight = 8;
            body.style.paddingTop   = 4;
            body.style.flexGrow     = 1;
            body.style.overflow     = Overflow.Hidden;

            var spkLbl = new Label(line?.Speaker != null ? line.Speaker.DisplayName : "나레이션");
            spkLbl.style.fontSize = 10;
            spkLbl.style.color    = new StyleColor(new Color(0.65f, 0.65f, 0.65f));
            body.Add(spkLbl);

            var txLbl = new Label(Trunc(line?.DialogueText, 34));
            txLbl.style.fontSize    = 10;
            txLbl.style.overflow    = Overflow.Hidden;
            txLbl.style.whiteSpace  = WhiteSpace.Normal;
            body.Add(txLbl);
            Add(body);

            // ── Ports ─────────────────────────────────
            _inputPort = MakePort(new Color(0.3f, 0.3f, 0.75f));
            Add(_inputPort);

            _outputPort = MakePort(new Color(0.3f, 0.75f, 0.3f));
            _outputPort.tooltip = "클릭 → 연결 시작  /  우클릭 → Disconnect";
            _outputPort.RegisterCallback<ClickEvent>(e =>
            {
                OutputPortClicked?.Invoke(this);
                e.StopPropagation();
            });
            _outputPort.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                bool hasNext = !string.IsNullOrWhiteSpace(Line?.NextLineId);
                evt.menu.AppendAction(
                    "Disconnect",
                    _ => DisconnectRequested?.Invoke(this),
                    hasNext ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }));
            Add(_outputPort);

            // ── Resize Handle ─────────────────────────
            _resizeHandle = new VisualElement();
            _resizeHandle.style.position        = Position.Absolute;
            _resizeHandle.style.width           = GripSize;
            _resizeHandle.style.height          = GripSize;
            _resizeHandle.style.backgroundColor = new StyleColor(new Color(0.35f, 0.35f, 0.35f, 0.7f));
            _resizeHandle.tooltip               = "드래그하여 크기 조절";
            _resizeHandle.generateVisualContent += DrawGrip;
            Add(_resizeHandle);

            // ── 이벤트 등록 ──────────────────────────
            RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(_ => CancelAll());

            _resizeHandle.RegisterCallback<PointerDownEvent>(OnResizePointerDown);
            _resizeHandle.RegisterCallback<PointerMoveEvent>(OnResizePointerMove);
            _resizeHandle.RegisterCallback<PointerUpEvent>(OnResizePointerUp);
            _resizeHandle.RegisterCallback<PointerCancelEvent>(_ => _resizing = false);

            SetSize(w, h);
        }

        // ── 공개 API ─────────────────────────────────

        public void SetSelected(bool on) =>
            ApplyBorder(on ? new Color(0.9f, 0.7f, 0.2f) : new Color(0.35f, 0.35f, 0.35f), on ? 2f : 1f);

        public void SetPendingSource(bool on) =>
            _outputPort.style.backgroundColor = new StyleColor(on
                ? new Color(0.2f, 1f, 0.3f)
                : new Color(0.3f, 0.75f, 0.3f));

        public void Refresh() => _idLabel.text = IdText(Line);

        // ── 포트 좌표 (캔버스 로컬) ─────────────────
        public Vector2 OutPos => new Vector2(layout.xMax, layout.y + layout.height * 0.5f);
        public Vector2 InPos  => new Vector2(layout.xMin, layout.y + layout.height * 0.5f);

        // ── SetSize ──────────────────────────────────

        private void SetSize(float w, float h)
        {
            _w = Mathf.Max(MinW, w);
            _h = Mathf.Max(MinH, h);
            style.width  = _w;
            style.height = _h;

            float portTop = _h * 0.5f - PortRadius;
            _inputPort.style.left  = -PortRadius;
            _inputPort.style.top   = portTop;
            _outputPort.style.left = _w - PortRadius;
            _outputPort.style.top  = portTop;

            _resizeHandle.style.left = _w - GripSize;
            _resizeHandle.style.top  = _h - GripSize;
        }

        // ── Resize Handle 드로잉 ─────────────────────

        private static void DrawGrip(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            p.strokeColor = new Color(0.75f, 0.75f, 0.75f, 0.9f);
            p.lineWidth   = 1.5f;
            float sz = GripSize;
            for (int i = 1; i <= 3; i++)
            {
                float d = i * (sz / 4f);
                p.BeginPath();
                p.MoveTo(new Vector2(sz, sz - d));
                p.LineTo(new Vector2(sz - d, sz));
                p.Stroke();
            }
        }

        // ── 드래그(이동) ─────────────────────────────

        private void OnPointerDown(PointerDownEvent e)
        {
            if (e.button != 0) return;
            if (e.target == _outputPort || e.target == _resizeHandle) return;
            if (IsConnectModeActive?.Invoke() == true) return;

            _dragging        = true;
            _dragMoved       = false;
            _dragStartScreen = e.position;
            _nodeStartPos    = new Vector2(resolvedStyle.left, resolvedStyle.top);
            this.CapturePointer(e.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent e)
        {
            if (!_dragging) return;
            Vector2 delta = (Vector2)e.position - _dragStartScreen;
            if (delta.magnitude < DragThreshold) return;
            _dragMoved = true;
            style.left = Mathf.Max(0f, _nodeStartPos.x + delta.x);
            style.top  = Mathf.Max(0f, _nodeStartPos.y + delta.y);
            e.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent e)
        {
            if (!_dragging) return;
            bool moved = _dragMoved;
            _dragging = false;
            this.ReleasePointer(e.pointerId);
            if (moved)
            {
                Vector2 delta = (Vector2)e.position - _dragStartScreen;
                Dragged?.Invoke(this, new Vector2(
                    Mathf.Max(0f, _nodeStartPos.x + delta.x),
                    Mathf.Max(0f, _nodeStartPos.y + delta.y)));
            }
        }

        // ── 리사이즈 ─────────────────────────────────

        private void OnResizePointerDown(PointerDownEvent e)
        {
            if (e.button != 0) return;
            if (IsConnectModeActive?.Invoke() == true) return;
            _resizing           = true;
            _resizeStartScreen  = e.position;
            _sizeAtResizeStart  = new Vector2(_w, _h);
            _resizeHandle.CapturePointer(e.pointerId);
            e.StopPropagation(); // 노드 드래그 시작 방지
        }

        private void OnResizePointerMove(PointerMoveEvent e)
        {
            if (!_resizing) return;
            Vector2 delta = (Vector2)e.position - _resizeStartScreen;
            SetSize(_sizeAtResizeStart.x + delta.x, _sizeAtResizeStart.y + delta.y);
            e.StopPropagation();
        }

        private void OnResizePointerUp(PointerUpEvent e)
        {
            if (!_resizing) return;
            _resizing = false;
            _resizeHandle.ReleasePointer(e.pointerId);
            Resized?.Invoke(this, new Vector2(_w, _h));
            e.StopPropagation();
        }

        // ── 클릭 ─────────────────────────────────────

        private void OnClick(ClickEvent e)
        {
            if (_dragMoved) { _dragMoved = false; return; }
            if (e.target == _outputPort) return;
            if (e.clickCount == 2) { DoubleClicked?.Invoke(this); e.StopPropagation(); return; }
            Clicked?.Invoke(this);
            e.StopPropagation();
        }

        // ── 내부 헬퍼 ────────────────────────────────

        private void CancelAll()
        {
            _dragging  = false;
            _dragMoved = false;
            _resizing  = false;
        }

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
            p.style.position  = Position.Absolute;
            p.style.width     = PortDiameter;
            p.style.height    = PortDiameter;
            p.style.backgroundColor = new StyleColor(col);
            p.style.borderTopLeftRadius    = p.style.borderTopRightRadius    =
            p.style.borderBottomLeftRadius = p.style.borderBottomRightRadius = PortRadius;
            return p;
        }

        private static string IdText(StoryLineSO l)   => string.IsNullOrWhiteSpace(l?.LineId) ? "⚠ ID 없음" : l.LineId;
        private static string Trunc(string s, int max) { if (string.IsNullOrEmpty(s)) return ""; s = s.Replace('\n',' '); return s.Length <= max ? s : s[..max]+"…"; }
    }
}
