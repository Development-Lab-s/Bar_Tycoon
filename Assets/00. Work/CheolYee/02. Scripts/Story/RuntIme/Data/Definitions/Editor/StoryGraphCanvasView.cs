using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 노드 박스 + 연결선을 그리는 캔버스 VisualElement.
    /// pan:  미들 마우스 드래그 (자연스러운 방향 – 드래그 방향으로 캔버스 이동)
    /// zoom: Ctrl + 마우스 휠 스크롤 (마우스 커서 위치 기준 zoom-in/out)
    /// </summary>
    public sealed class StoryGraphCanvasView : VisualElement
    {
        // ── 레이아웃 상수 ────────────────────────────
        private const float StartX             = 40f;
        private const float StartY             = 30f;
        private const float StepY              = 110f;
        private const float CanvasMinW         = 6000f;
        private const float CanvasMinH         = 4000f;
        private const float EdgeHitThreshold   = 8f;
        private const float InputPortHitRadius = 18f;
        private const float DragConnectThresh  = 6f;
        private const float ChoiceOffsetX      = 20f;
        private const float ChoiceOffsetY      = 10f;

        // ── 줌 상수 ──────────────────────────────────
        public const float MinZoom = 0.4f;
        public const float MaxZoom = 2.0f;

        // ── 이벤트 ───────────────────────────────────
        public event Action<StoryGraphNodeView, StoryGraphNodeView> ConnectionMade;
        public event Action<StoryGraphNodeView> NodeSelected;
        public event Action<StoryGraphNodeView> NodeDisconnected;

        // ── UI 요소 참조 ─────────────────────────────
        private readonly VisualElement            _canvas;
        private readonly VisualElement            _viewport;
        private readonly List<StoryGraphNodeView> _nodes = new();

        // ── Choice 노드 ──────────────────────────────
        private readonly List<StoryChoiceNodeView>                           _choiceNodes  = new();
        private readonly Dictionary<StoryGraphNodeView, StoryChoiceNodeView> _nodeToChoice = new();

        // ── 선택 상태 ────────────────────────────────
        private StoryGraphNodeView _selectedNode;
        private StoryGraphNodeView _selectedEdgeSrc;

        // ── 연결 상태 (라인 노드 → 라인 노드) ──────────
        private StoryGraphNodeView _pendingSource;
        private bool               _isDragConnecting;
        private Vector2            _dragConnectCurrentPos;

        // ── 연결 상태 (choice option → 라인 노드) ──────
        private StoryChoiceNodeView _pendingChoiceNode;
        private int                 _pendingChoiceOptIdx;
        private bool                _isChoiceDragConnecting;

        // ── 뷰 상태 (pan + zoom) ──────────────────────
        private bool    _isPanning;
        private Vector2 _panStartMouse;
        private Vector2 _panOffset;
        private Vector2 _panStartOffset;
        private float   _zoomScale = 1f;

        // ── 스무스 zoom 상태 ──────────────────────────
        private float   _zoomTarget;
        private Vector2 _zoomAnchorLocal;    // StoryGraphCanvasView 로컬 좌표
        private Vector2 _zoomAnchorCanvas;   // 고정할 캔버스 좌표
        private IVisualElementScheduledItem _zoomSchedule;

        // ── 생성자 ───────────────────────────────────

        public StoryGraphCanvasView()
        {
            style.flexGrow = 1;

            _viewport = new VisualElement();
            _viewport.style.flexGrow = 1;
            _viewport.style.overflow = Overflow.Hidden;

            _canvas = new VisualElement();
            _canvas.style.position = Position.Absolute;
            _canvas.style.left     = 0;
            _canvas.style.top      = 0;
            _canvas.style.width    = CanvasMinW;
            _canvas.style.height   = CanvasMinH;
            _canvas.style.transformOrigin = new TransformOrigin(0, 0);
            _canvas.generateVisualContent += DrawConnections;
            _canvas.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown);

            _viewport.Add(_canvas);
            Add(_viewport);

            RegisterCallback<PointerDownEvent>(OnOuterPointerDown);
            RegisterCallback<PointerMoveEvent>(OnOuterPointerMove);
            RegisterCallback<PointerUpEvent>(OnOuterPointerUp);
            RegisterCallback<PointerCancelEvent>(_ => CancelAllOuter());
            _viewport.RegisterCallback<WheelEvent>(OnWheel);
        }

        // ── 공개 API ─────────────────────────────────

        public void Rebuild(IReadOnlyList<StoryLineSO> lines)
        {
            _canvas.Clear();
            _nodes.Clear();
            _choiceNodes.Clear();
            _nodeToChoice.Clear();
            _selectedNode           = null;
            _selectedEdgeSrc        = null;
            _pendingSource          = null;
            _isDragConnecting       = false;
            _pendingChoiceNode      = null;
            _isChoiceDragConnecting = false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == null) continue;
                var node = CreateNode(lines[i]);

                if (lines[i].HasEditorNodePosition)
                {
                    var saved = lines[i].EditorNodePosition;
                    node.style.left = saved.x;
                    node.style.top  = saved.y;
                }
                else
                {
                    node.style.left = StartX;
                    node.style.top  = StartY + i * StepY;
                }

                _canvas.Add(node);
                _nodes.Add(node);

                var choice = FindChoiceModule(lines[i]);
                if (choice != null)
                {
                    var choiceNode = CreateChoiceNode(node, choice, lines[i]);
                    _canvas.Add(choiceNode);
                    _choiceNodes.Add(choiceNode);
                    _nodeToChoice[node] = choiceNode;

                    // 레이아웃 완료 후 정확한 위치 배치
                    node.RegisterCallback<GeometryChangedEvent>(_ => RepositionChoiceNode(node));
                }
            }

            _canvas.MarkDirtyRepaint();
        }

        public void SelectNode(StoryLineSO line)
        {
            foreach (var n in _nodes)
            {
                bool isTarget = n.Line == line;
                n.SetSelected(isTarget);
                if (isTarget) _selectedNode = n;
            }
            _canvas.MarkDirtyRepaint();
        }

        public void RefreshAll()
        {
            foreach (var n  in _nodes)       n.Refresh();
            foreach (var cn in _choiceNodes) cn.Refresh();
            _canvas.MarkDirtyRepaint();
        }

        public void RefreshNodePositions()
        {
            foreach (var n in _nodes)
            {
                if (n.Line == null) continue;
                if (n.Line.HasEditorNodePosition)
                {
                    var pos = n.Line.EditorNodePosition;
                    n.style.left = pos.x;
                    n.style.top  = pos.y;
                }
                if (_nodeToChoice.ContainsKey(n))
                    RepositionChoiceNode(n);
            }
            _canvas.MarkDirtyRepaint();
        }

        public StoryGraphNodeView SelectedNode    => _selectedNode;
        public StoryGraphNodeView SelectedEdgeSrc => _selectedEdgeSrc;

        public void ClearSelectedEdge()
        {
            _selectedEdgeSrc = null;
            _canvas.MarkDirtyRepaint();
        }

        // ── Ctrl + 마우스 휠 줌 (WheelEvent, 스무스) ─

        private void OnWheel(WheelEvent e)
        {
            if (!e.ctrlKey && !e.commandKey) return;

            _zoomAnchorLocal  = e.localMousePosition;
            _zoomAnchorCanvas = (_zoomAnchorLocal - _panOffset) / _zoomScale;

            float baseScale = _zoomSchedule != null ? _zoomTarget : _zoomScale;
            float factor    = e.delta.y < 0f ? 1.1f : (1f / 1.1f);
            _zoomTarget     = Mathf.Clamp(baseScale * factor, MinZoom, MaxZoom);

            _zoomSchedule?.Pause();
            _zoomSchedule = schedule.Execute(SmoothZoomTick).Every(16);

            e.StopPropagation();
        }

        /// <summary>
        /// 16ms 간격 zoom 애니메이션 틱.
        /// exponential smoothing: 매 프레임 잔여 거리의 ~21% 이동 (60fps 기준).
        /// </summary>
        private void SmoothZoomTick(TimerState ts)
        {
            float dt       = ts.deltaTime / 1000f;
            float newScale = Mathf.Lerp(_zoomScale, _zoomTarget,
                                        1f - Mathf.Exp(-15f * dt));

            bool done = Mathf.Abs(newScale - _zoomTarget) < 0.0005f;
            if (done) newScale = _zoomTarget;

            _zoomScale = newScale;
            _panOffset = _zoomAnchorLocal - _zoomAnchorCanvas * _zoomScale;

            ApplyTransform();
            UpdateNodeZoom();

            if (done)
            {
                _zoomSchedule?.Pause();
                _zoomSchedule = null;
            }
        }

        // ── 뷰 상태 저장/로드 (EditorPrefs, per-episode) ─

        public void SaveViewState(string prefsKey)
        {
            if (string.IsNullOrEmpty(prefsKey)) return;
            EditorPrefs.SetFloat(prefsKey + "_PanX", _panOffset.x);
            EditorPrefs.SetFloat(prefsKey + "_PanY", _panOffset.y);
            EditorPrefs.SetFloat(prefsKey + "_Zoom", _zoomScale);
        }

        public void LoadViewState(string prefsKey)
        {
            if (string.IsNullOrEmpty(prefsKey)) return;
            _panOffset.x = EditorPrefs.GetFloat(prefsKey + "_PanX", 0f);
            _panOffset.y = EditorPrefs.GetFloat(prefsKey + "_PanY", 0f);
            _zoomScale   = Mathf.Clamp(EditorPrefs.GetFloat(prefsKey + "_Zoom", 1f), MinZoom, MaxZoom);
            ApplyTransform();
            UpdateNodeZoom();
        }

        // ── 노드 생성 ────────────────────────────────

        private StoryGraphNodeView CreateNode(StoryLineSO line)
        {
            var node = new StoryGraphNodeView(line);
            node.ZoomScale           = _zoomScale;
            node.IsConnectModeActive = () => _isPanning || _pendingSource != null || _pendingChoiceNode != null;
            node.Clicked             += OnNodeClicked;
            node.DoubleClicked       += OnNodeDoubleClicked;
            node.OutputPortDragStart += OnOutputPortDragStart;
            node.Dragged             += OnNodeDragged;
            node.Resized             += OnNodeResized;
            node.DisconnectRequested += OnNodeDisconnectRequested;

            node.RegisterCallback<GeometryChangedEvent>(_ => _canvas.MarkDirtyRepaint());
            return node;
        }

        private StoryChoiceNodeView CreateChoiceNode(StoryGraphNodeView parent, StoryChoiceModuleSO choice, StoryLineSO line)
        {
            var cn = new StoryChoiceNodeView(line, choice);
            cn.ZoomScale             = _zoomScale;
            cn.ReactionPortDragStart += OnChoiceReactionPortDragStart;
            cn.Changed               += () => _canvas.MarkDirtyRepaint();
            cn.RegisterCallback<GeometryChangedEvent>(_ => _canvas.MarkDirtyRepaint());
            return cn;
        }

        private void RepositionChoiceNode(StoryGraphNodeView parent)
        {
            if (!_nodeToChoice.TryGetValue(parent, out var cn)) return;
            float left  = parent.resolvedStyle.left;
            float top   = parent.resolvedStyle.top;
            float width = parent.resolvedStyle.width;
            if (float.IsNaN(left) || float.IsNaN(top)) return;
            if (float.IsNaN(width) || width <= 0f) width = StoryGraphNodeView.DefaultW;
            cn.style.left = left  + width + ChoiceOffsetX;
            cn.style.top  = top   + ChoiceOffsetY;
        }

        private void OnNodeDragged(StoryGraphNodeView node, Vector2 pos)
        {
            StoryEditorUtility.SetEditorNodePosition(node.Line, pos, recordUndo: true);
            if (_nodeToChoice.TryGetValue(node, out var cn))
            {
                float w = node.resolvedStyle.width;
                if (float.IsNaN(w) || w <= 0f) w = StoryGraphNodeView.DefaultW;
                cn.style.left = pos.x + w + ChoiceOffsetX;
                cn.style.top  = pos.y + ChoiceOffsetY;
            }
            _canvas.MarkDirtyRepaint();
        }

        private void OnNodeResized(StoryGraphNodeView node, Vector2 size)
        {
            StoryEditorUtility.SetEditorNodeSize(node.Line, size, recordUndo: true);
            if (_nodeToChoice.ContainsKey(node))
                RepositionChoiceNode(node);
            _canvas.MarkDirtyRepaint();
        }

        // ── 노드 클릭 핸들러 ─────────────────────────

        private void OnNodeClicked(StoryGraphNodeView node)
        {
            // choice option click-to-connect 완료
            if (_pendingChoiceNode != null && !_isChoiceDragConnecting)
            {
                SetChoiceOptionReaction(_pendingChoiceNode, _pendingChoiceOptIdx, node.Line);
                ClearChoicePending();
                return;
            }

            if (_pendingSource != null && !_isDragConnecting)
            {
                if (_pendingSource != node)
                    ConnectionMade?.Invoke(_pendingSource, node);
                ClearPending();
                return;
            }

            _selectedEdgeSrc = null;
            SetSelected(node);
            NodeSelected?.Invoke(node);
        }

        private void OnNodeDoubleClicked(StoryGraphNodeView node)
        {
            EditorGUIUtility.PingObject(node.Line);
            Selection.activeObject = node.Line;
        }

        // ── 출력 포트 드래그 시작 ────────────────────

        private void OnOutputPortDragStart(StoryGraphNodeView node)
        {
            ClearPending();
            ClearChoicePending();
            _pendingSource         = node;
            _isDragConnecting      = false;
            _dragConnectCurrentPos = node.OutPos;
            node.SetPendingSource(true);

            this.CapturePointer(PointerId.mousePointerId);
            _canvas.MarkDirtyRepaint();
        }

        // ── Choice option 포트 드래그 시작 ───────────

        private void OnChoiceReactionPortDragStart(StoryChoiceNodeView choiceNode, int optIdx)
        {
            ClearPending();
            ClearChoicePending();
            _pendingChoiceNode      = choiceNode;
            _pendingChoiceOptIdx    = optIdx;
            _isChoiceDragConnecting = false;
            _dragConnectCurrentPos  = choiceNode.ReactionPortPos(optIdx);

            this.CapturePointer(PointerId.mousePointerId);
            _canvas.MarkDirtyRepaint();
        }

        // ── 캔버스 포인터 다운 (edge 선택) ───────────

        private void OnCanvasPointerDown(PointerDownEvent e)
        {
            if (e.button != 0) return;
            if (e.target != _canvas) return;

            Vector2 localPos = _canvas.WorldToLocal(e.position);

            var hitSrc = FindEdgeAt(localPos);
            if (hitSrc != null)
            {
                _selectedEdgeSrc = hitSrc;
                SetSelected(null);
                NodeSelected?.Invoke(null);
                _canvas.MarkDirtyRepaint();
                e.StopPropagation();
                return;
            }

            ClearPending();
            ClearChoicePending();
            SetSelected(null);
            _selectedEdgeSrc = null;
        }

        // ── Disconnect ──────────────────────────────

        private void OnNodeDisconnectRequested(StoryGraphNodeView node)
        {
            if (node.Line == null || string.IsNullOrWhiteSpace(node.Line.NextLineId)) return;
            Undo.RecordObject(node.Line, "Disconnect nextLineId");
            StoryEditorUtility.SetNextLineId(node.Line, "");
            node.Refresh();
            _canvas.MarkDirtyRepaint();
            NodeDisconnected?.Invoke(node);
        }

        // ── 외부 포인터 이벤트 (pan + drag-connect) ──

        private void OnOuterPointerDown(PointerDownEvent e)
        {
            if (e.button == 2)
            {
                _isPanning      = true;
                _panStartMouse  = e.position;
                _panStartOffset = _panOffset;
                this.CapturePointer(e.pointerId);
                e.StopPropagation();
            }
        }

        private void OnOuterPointerMove(PointerMoveEvent e)
        {
            if (_isPanning)
            {
                Vector2 delta = (Vector2)e.position - _panStartMouse;
                _panOffset = _panStartOffset + delta;
                ApplyTransform();
                e.StopPropagation();
                return;
            }

            if (_pendingSource != null)
            {
                _dragConnectCurrentPos = _canvas.WorldToLocal(e.position);
                if (Vector2.Distance(_dragConnectCurrentPos, _pendingSource.OutPos) > DragConnectThresh)
                    _isDragConnecting = true;
                _canvas.MarkDirtyRepaint();
                return;
            }

            if (_pendingChoiceNode != null)
            {
                _dragConnectCurrentPos = _canvas.WorldToLocal(e.position);
                var portPos = _pendingChoiceNode.ReactionPortPos(_pendingChoiceOptIdx);
                if (Vector2.Distance(_dragConnectCurrentPos, portPos) > DragConnectThresh)
                    _isChoiceDragConnecting = true;
                _canvas.MarkDirtyRepaint();
            }
        }

        private void OnOuterPointerUp(PointerUpEvent e)
        {
            if (e.button == 2 && _isPanning)
            {
                _isPanning = false;
                this.ReleasePointer(e.pointerId);
                e.StopPropagation();
                return;
            }

            if (_pendingSource != null && e.button == 0)
            {
                this.ReleasePointer(e.pointerId);

                if (_isDragConnecting)
                {
                    Vector2 canvasPos = _canvas.WorldToLocal(e.position);
                    var target = FindNodeNearInputPort(canvasPos);
                    if (target != null && target != _pendingSource)
                        ConnectionMade?.Invoke(_pendingSource, target);
                    ClearPending();
                    e.StopPropagation();
                }

                _isDragConnecting = false;
                _canvas.MarkDirtyRepaint();
                return;
            }

            if (_pendingChoiceNode != null && e.button == 0)
            {
                this.ReleasePointer(e.pointerId);

                if (_isChoiceDragConnecting)
                {
                    Vector2 canvasPos = _canvas.WorldToLocal(e.position);
                    var target = FindNodeNearInputPort(canvasPos);
                    if (target != null)
                        SetChoiceOptionReaction(_pendingChoiceNode, _pendingChoiceOptIdx, target.Line);
                    e.StopPropagation();
                }

                ClearChoicePending();
                _canvas.MarkDirtyRepaint();
            }
        }

        private void CancelAllOuter()
        {
            if (_isPanning)             { _isPanning = false; }
            if (_isDragConnecting)      { ClearPending();       _isDragConnecting       = false; }
            if (_isChoiceDragConnecting){ ClearChoicePending(); _isChoiceDragConnecting = false; }
        }

        // ── Choice reaction 설정 ─────────────────────

        private static void SetChoiceOptionReaction(StoryChoiceNodeView choiceNode, int optIdx, StoryLineSO targetLine)
        {
            if (choiceNode?.Choice == null || targetLine == null) return;
            Undo.RecordObject(choiceNode.Choice, "Connect Choice Reaction");
            var so = new SerializedObject(choiceNode.Choice);
            so.Update();
            so.FindProperty("options")
              .GetArrayElementAtIndex(optIdx)
              .FindPropertyRelative("reactionStartLineId")
              .stringValue = targetLine.LineId;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(choiceNode.Choice);
            choiceNode.Refresh();
        }

        // ── 상태 헬퍼 ────────────────────────────────

        private void SetSelected(StoryGraphNodeView node)
        {
            if (_selectedNode != null) _selectedNode.SetSelected(false);
            _selectedNode = node;
            if (_selectedNode != null) _selectedNode.SetSelected(true);
            _canvas.MarkDirtyRepaint();
        }

        private void ClearPending()
        {
            if (_pendingSource != null)
            {
                _pendingSource.SetPendingSource(false);
                _pendingSource = null;
            }
            _isDragConnecting      = false;
            _dragConnectCurrentPos = Vector2.zero;
            _canvas.MarkDirtyRepaint();
        }

        private void ClearChoicePending()
        {
            _pendingChoiceNode      = null;
            _pendingChoiceOptIdx    = 0;
            _isChoiceDragConnecting = false;
            _dragConnectCurrentPos  = Vector2.zero;
        }

        private void ApplyTransform()
        {
            _canvas.style.translate = new Vector3(_panOffset.x, _panOffset.y, 0f);
            _canvas.style.scale    = new Vector3(_zoomScale, _zoomScale, 1f);
            _canvas.MarkDirtyRepaint();
        }

        private void UpdateNodeZoom()
        {
            foreach (var n  in _nodes)       n.ZoomScale  = _zoomScale;
            foreach (var cn in _choiceNodes) cn.ZoomScale = _zoomScale;
        }

        // ── 연결선 드로잉 ────────────────────────────

        private void DrawConnections(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;

            // 라인 노드 → 라인 노드 연결선
            foreach (var src in _nodes)
            {
                if (_isDragConnecting && src == _pendingSource) continue;

                string nextId = src.Line?.NextLineId;
                if (string.IsNullOrWhiteSpace(nextId)) continue;

                var dst = FindNodeById(nextId);
                if (dst == null) continue;

                bool selected = src == _selectedEdgeSrc;
                DrawBezier(p, src.OutPos, dst.InPos,
                    selected ? new Color(1f, 0.8f, 0.2f, 0.95f) : new Color(0.4f, 0.8f, 0.4f, 0.85f),
                    selected ? 3f : 2f);
            }

            // 라인 노드 → choice 노드 부모 링크 (얇은 앰버 선)
            foreach (var kv in _nodeToChoice)
            {
                Vector2 from = NodeBottomCenter(kv.Key);
                Vector2 to   = ChoiceNodeTopCenter(kv.Value);
                DrawBezier(p, from, to, new Color(0.72f, 0.52f, 0.18f, 0.55f), 1.5f);
            }

            // choice option → reaction 대상 라인 노드 연결선
            foreach (var choiceNode in _choiceNodes)
            {
                if (choiceNode.Choice == null) continue;
                for (int i = 0; i < choiceNode.Choice.Options.Count; i++)
                {
                    if (_isChoiceDragConnecting && choiceNode == _pendingChoiceNode && i == _pendingChoiceOptIdx)
                        continue;

                    string reactionId = choiceNode.Choice.Options[i].reactionStartLineId;
                    if (string.IsNullOrWhiteSpace(reactionId)) continue;

                    var dst = FindNodeById(reactionId);
                    if (dst == null) continue;

                    DrawBezier(p, choiceNode.ReactionPortPos(i), dst.InPos,
                        new Color(0.3f, 0.8f, 0.35f, 0.85f), 2f);
                }
            }

            // 드래그 preview 선 (라인 노드 포트)
            if (_pendingSource != null && _isDragConnecting && _dragConnectCurrentPos != Vector2.zero)
            {
                DrawBezier(p, _pendingSource.OutPos, _dragConnectCurrentPos,
                    new Color(0.9f, 0.9f, 0.3f, 0.7f), 1.5f);
            }

            // 드래그 preview 선 (choice option 포트)
            if (_pendingChoiceNode != null && _isChoiceDragConnecting && _dragConnectCurrentPos != Vector2.zero)
            {
                DrawBezier(p,
                    _pendingChoiceNode.ReactionPortPos(_pendingChoiceOptIdx),
                    _dragConnectCurrentPos,
                    new Color(0.3f, 0.9f, 0.4f, 0.7f), 1.5f);
            }
        }

        private static Vector2 NodeBottomCenter(StoryGraphNodeView node)
        {
            return new Vector2(
                node.layout.xMin + node.layout.width  * 0.5f,
                node.layout.yMax);
        }

        private static Vector2 ChoiceNodeTopCenter(StoryChoiceNodeView node)
        {
            return new Vector2(
                node.layout.xMin + StoryChoiceNodeView.NodeW * 0.5f,
                node.layout.yMin);
        }

        private void DrawBezier(Painter2D p, Vector2 from, Vector2 to, Color color, float lineWidth = 2f)
        {
            float dx     = Mathf.Abs(to.x - from.x);
            float tanLen = Mathf.Max(dx * 0.5f, 60f);
            var c1 = new Vector2(from.x + tanLen, from.y);
            var c2 = new Vector2(to.x   - tanLen, to.y);

            p.strokeColor = color;
            p.lineWidth   = lineWidth;
            p.BeginPath();
            p.MoveTo(from);
            p.BezierCurveTo(c1, c2, to);
            p.Stroke();
        }

        // ── Edge Hit-Test ────────────────────────────

        private StoryGraphNodeView FindEdgeAt(Vector2 canvasPos)
        {
            foreach (var src in _nodes)
            {
                string nextId = src.Line?.NextLineId;
                if (string.IsNullOrWhiteSpace(nextId)) continue;
                var dst = FindNodeById(nextId);
                if (dst == null) continue;

                if (IsNearBezier(src.OutPos, dst.InPos, canvasPos, EdgeHitThreshold))
                    return src;
            }
            return null;
        }

        private static bool IsNearBezier(Vector2 from, Vector2 to, Vector2 point, float threshold)
        {
            float dx     = Mathf.Abs(to.x - from.x);
            float tanLen = Mathf.Max(dx * 0.5f, 60f);
            var c1 = new Vector2(from.x + tanLen, from.y);
            var c2 = new Vector2(to.x   - tanLen, to.y);

            for (int i = 0; i <= 24; i++)
            {
                float t = i / 24f;
                var   q = CubicBezier(from, c1, c2, to, t);
                if (Vector2.Distance(q, point) <= threshold) return true;
            }
            return false;
        }

        private static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            return u*u*u*p0 + 3f*u*u*t*p1 + 3f*u*t*t*p2 + t*t*t*p3;
        }

        // ── Input Port 근처 노드 탐색 ────────────────

        private StoryGraphNodeView FindNodeNearInputPort(Vector2 canvasPos)
        {
            StoryGraphNodeView best     = null;
            float              bestDist = InputPortHitRadius;

            foreach (var n in _nodes)
            {
                float d = Vector2.Distance(n.InPos, canvasPos);
                if (d < bestDist) { bestDist = d; best = n; }
            }
            return best;
        }

        // ── 탐색 유틸 ────────────────────────────────

        private StoryGraphNodeView FindNodeById(string lineId)
        {
            foreach (var n in _nodes)
                if (n.Line != null && n.Line.LineId == lineId) return n;
            return null;
        }

        private static StoryChoiceModuleSO FindChoiceModule(StoryLineSO line)
        {
            if (line?.Modules == null) return null;
            foreach (var m in line.Modules)
                if (m is StoryChoiceModuleSO c) return c;
            return null;
        }
    }
}
