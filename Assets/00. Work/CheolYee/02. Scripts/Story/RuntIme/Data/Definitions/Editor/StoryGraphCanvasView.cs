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
    /// multi-select: Ctrl+클릭 토글 / 박스 드래그(marquee)
    /// multi-move:   선택된 노드 전체를 동일 delta로 이동
    /// </summary>
    public sealed class StoryGraphCanvasView : VisualElement
    {
        // ── 레이아웃 상수 ────────────────────────────
        private const float StartX             = 40f;
        private const float StartY             = 30f;
        private const float StepY              = 110f;
        private const float CanvasMinW         = 12000f;
        private const float CanvasMinH         = 8000f;
        private const float PanMargin          = 200f;   // viewport 안에 항상 최소 이 px만큼 canvas 유지
        private const float EdgeHitThreshold   = 8f;
        private const float InputPortHitRadius = 18f;
        private const float DragConnectThresh  = 6f;

        // ── 그리드 상수 ──────────────────────────────
        private const float GridMinor          = 20f;    // canvas-space 마이너 간격
        private const float GridMajor          = 100f;   // canvas-space 메이저 간격
        private const float GridMinorHideBelow = 5f;     // 화면상 px 미만이면 마이너 생략

        // ── 줌 상수 ──────────────────────────────────
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 2.2f;

        // ── 이벤트 ───────────────────────────────────
        public event Action<StoryGraphNodeView, StoryGraphNodeView> ConnectionMade;
        /// <summary>선택 상태 변경. 빈 리스트 = 선택 없음.</summary>
        public event Action<IReadOnlyList<StoryGraphNodeView>> SelectionChanged;
        public event Action<StoryGraphNodeView> NodeDisconnected;
        /// <summary>빈 캔버스 공간 우클릭 → 컨텍스트 메뉴에서 "새 라인 생성" 선택 시. canvas-space 좌표.</summary>
        public event Action<Vector2> CreateLineRequested;

        // ── UI 요소 참조 ─────────────────────────────
        private readonly VisualElement            _canvas;
        private readonly VisualElement            _viewport;
        private readonly List<StoryGraphNodeView> _nodes = new();

        // ── 모듈 스택 ─────────────────────────────────
        private readonly List<StoryNodeModuleStackView>                           _moduleStacks      = new();
        private readonly Dictionary<StoryGraphNodeView, StoryNodeModuleStackView> _nodeToModuleStack = new();

        // ── 선택 상태 ────────────────────────────────
        private readonly HashSet<StoryGraphNodeView> _selectedNodes = new();
        private StoryGraphNodeView _primaryNode;       // inspector 표시용 (단일 선택 시 해당 노드)
        private StoryGraphNodeView _selectedEdgeSrc;

        // ── 연결 상태 (라인 노드 → 라인 노드) ──────────
        private StoryGraphNodeView _pendingSource;
        private bool               _isDragConnecting;
        private Vector2            _dragConnectCurrentPos;

        // ── 연결 상태 (connectable 포트 → 라인 노드) ───
        private StoryNodeModuleStackView _pendingConnectableStack;
        private int                      _pendingConnectableOptIdx;
        private bool                     _isConnectableDragConnecting;

        // ── connectable 엣지 선택 상태 ─────────────────
        private StoryNodeModuleStackView _selectedConnectableEdgeStack;
        private int                      _selectedConnectableEdgeOptIdx = -1;

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

        // ── 박스 선택 (marquee) 상태 ──────────────────
        private bool    _isMarqueeSelecting;
        private Vector2 _marqueeStart;   // canvas 로컬 좌표
        private Vector2 _marqueeEnd;     // canvas 로컬 좌표
        private int     _marqueePointerId = -1;

        // ── 다중 이동 상태 ────────────────────────────
        private readonly Dictionary<StoryGraphNodeView, Vector2> _dragStartPositions = new();

        // ── 마우스 위치 / 호버 추적 ─────────────────────────
        private Vector2 _canvasMousePos;
        private bool    _isPointerOverViewport;
        private int     _childHoverCount;

        // ── 프리뷰 하이라이트 ────────────────────────────────
        private string _previewLineId;

        // ── 생성자 ───────────────────────────────────

        public StoryGraphCanvasView()
        {
            style.flexGrow = 1;

            _viewport = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    overflow = Overflow.Hidden
                }
            };
            _viewport.generateVisualContent += DrawBackground;
            _viewport.RegisterCallback<PointerEnterEvent>(_ => _isPointerOverViewport = true);
            _viewport.RegisterCallback<PointerLeaveEvent>(_ => _isPointerOverViewport = false);
            _viewport.RegisterCallback<PointerMoveEvent>(e =>
                _canvasMousePos = _canvas.WorldToLocal(e.position));

            _canvas = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    width = CanvasMinW,
                    height = CanvasMinH,
                    transformOrigin = new TransformOrigin(0, 0)
                }
            };
            _canvas.generateVisualContent += DrawConnections;
            _canvas.RegisterCallback<PointerDownEvent>(OnCanvasPointerDown);
            _canvas.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                // 빈 공간에서만 "새 라인 생성" 표시 (노드/스택 위 우클릭 시 IsPointerOverChild = true)
                if (IsPointerOverChild) return;
                evt.menu.AppendAction("새 라인 생성", _ => CreateLineRequested?.Invoke(_canvasMousePos));
            }));

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
            _moduleStacks.Clear();
            _nodeToModuleStack.Clear();
            ClearAllSelected();
            _selectedEdgeSrc               = null;
            _pendingSource                 = null;
            _isDragConnecting              = false;
            _pendingConnectableStack       = null;
            _isConnectableDragConnecting   = false;
            _isMarqueeSelecting            = false;
            _dragStartPositions.Clear();
            _childHoverCount               = 0;
            _isPointerOverViewport         = false;

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

                var ms = CreateModuleStack(node, lines[i]);
                _canvas.Add(ms);
                _moduleStacks.Add(ms);
                _nodeToModuleStack[node] = ms;
                node.RegisterCallback<GeometryChangedEvent>(_ => RepositionModuleStack(node));
            }

            _canvas.MarkDirtyRepaint();
        }

        public void SelectNode(StoryLineSO line)
        {
            ClearAllSelected();
            foreach (var n in _nodes)
            {
                if (n.Line == line)
                {
                    AddToSelection(n);
                    break;
                }
            }
            FireSelectionChanged();
            _canvas.MarkDirtyRepaint();
        }

        public void RefreshAll()
        {
            foreach (var n  in _nodes)        n.Refresh();
            foreach (var ms in _moduleStacks) ms.Refresh();
            foreach (var n  in _nodes)        RepositionModuleStack(n);
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
                RepositionModuleStack(n);
            }
            _canvas.MarkDirtyRepaint();
        }

        // ── 단일 선택 접근자 (이전 코드 호환) ──────────

        /// <summary>단일 선택 시 해당 노드, 다중/미선택 시 null.</summary>
        public StoryGraphNodeView SelectedNode    => _selectedNodes.Count == 1 ? _primaryNode : null;
        public StoryGraphNodeView SelectedEdgeSrc => _selectedEdgeSrc;

        // ── 마우스 / 호버 공개 API ────────────────────
        public Vector2 CanvasMousePosition   => _canvasMousePos;
        public bool    IsPointerOverViewport => _isPointerOverViewport;
        public bool    IsPointerOverChild    => _childHoverCount > 0;

        // ── 프리뷰 하이라이트 ─────────────────────────
        public void SetPreviewLine(string lineId)
        {
            _previewLineId = lineId;
            foreach (var n in _nodes)
                n.SetPreviewActive(n.Line?.LineId == lineId);
            _canvas.MarkDirtyRepaint();
        }

        public void ClearSelectedEdge()
        {
            _selectedEdgeSrc = null;
            _canvas.MarkDirtyRepaint();
        }

        public bool HasSelectedConnectableEdge => _selectedConnectableEdgeStack != null;

        private void ClearSelectedConnectableEdge()
        {
            _selectedConnectableEdgeStack  = null;
            _selectedConnectableEdgeOptIdx = -1;
            _canvas.MarkDirtyRepaint();
        }

        public void DisconnectSelectedConnectableEdge()
        {
            if (_selectedConnectableEdgeStack == null) return;
            var stack  = _selectedConnectableEdgeStack;
            var optIdx = _selectedConnectableEdgeOptIdx;
            ClearSelectedConnectableEdge();
            stack.ApplyDisconnect(optIdx);
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
            var node = new StoryGraphNodeView(line)
            {
                ZoomScale = _zoomScale,
                isConnectModeActive = () => _isPanning || _pendingSource != null || _pendingConnectableStack != null
            };
            node.Clicked             += OnNodeClicked;
            node.DoubleClicked       += OnNodeDoubleClicked;
            node.OutputPortDragStart += OnOutputPortDragStart;
            node.DragStarted         += OnNodeDragStarted;
            node.DragTotalDelta      += OnNodeDragTotalDelta;
            node.Dragged             += OnNodeDragged;
            node.Resized             += OnNodeResized;
            node.DisconnectRequested += OnNodeDisconnectRequested;

            node.RegisterCallback<GeometryChangedEvent>(_ => _canvas.MarkDirtyRepaint());
            node.RegisterCallback<PointerEnterEvent>(_ => _childHoverCount++);
            node.RegisterCallback<PointerLeaveEvent>(_ => _childHoverCount = Mathf.Max(0, _childHoverCount - 1));
            return node;
        }

        private StoryNodeModuleStackView CreateModuleStack(StoryGraphNodeView node, StoryLineSO line)
        {
            var ms = new StoryNodeModuleStackView(line);
            ms.Changed += () =>
            {
                ms.Refresh();
                RepositionModuleStack(node);
                _canvas.MarkDirtyRepaint();
            };
            ms.ConnectablePortDragStart += OnConnectablePortDragStart;
            ms.RegisterCallback<GeometryChangedEvent>(_ => _canvas.MarkDirtyRepaint());
            ms.RegisterCallback<PointerEnterEvent>(_ => _childHoverCount++);
            ms.RegisterCallback<PointerLeaveEvent>(_ => _childHoverCount = Mathf.Max(0, _childHoverCount - 1));
            return ms;
        }

        private void RepositionModuleStack(StoryGraphNodeView node)
        {
            if (!_nodeToModuleStack.TryGetValue(node, out var ms)) return;
            float left = node.resolvedStyle.left;
            float top  = node.resolvedStyle.top;
            if (float.IsNaN(left) || float.IsNaN(top)) return;
            float w = node.resolvedStyle.width;
            if (float.IsNaN(w) || w <= 0f) w = StoryGraphNodeView.DefaultW;
            ms.style.left  = left;
            ms.style.top   = top + node.CardHeight;
            ms.style.width = w;
        }

        // ── 드래그 이벤트 핸들러 (다중 이동 지원) ────────

        private void OnNodeDragStarted(StoryGraphNodeView node)
        {
            _dragStartPositions.Clear();

            // 드래그하는 노드가 선택 안 됐으면 단독 이동
            var targets = _selectedNodes.Contains(node) ? _selectedNodes : null;

            if (targets != null)
                foreach (var n in targets)
                    _dragStartPositions[n] = new Vector2(n.resolvedStyle.left, n.resolvedStyle.top);
            else
                _dragStartPositions[node] = new Vector2(node.resolvedStyle.left, node.resolvedStyle.top);
        }

        private void OnNodeDragTotalDelta(StoryGraphNodeView primary, Vector2 totalDelta)
        {
            if (!_dragStartPositions.TryGetValue(primary, out _)) return;
            if (_dragStartPositions.Count <= 1) return;   // 단일 이동은 노드 스스로 처리

            foreach (var kvp in _dragStartPositions)
            {
                var n = kvp.Key;
                if (n == primary) continue;
                var newPos = new Vector2(
                    Mathf.Max(0f, kvp.Value.x + totalDelta.x),
                    Mathf.Max(0f, kvp.Value.y + totalDelta.y));
                n.style.left = newPos.x;
                n.style.top  = newPos.y;
                if (_nodeToModuleStack.TryGetValue(n, out var ms))
                {
                    ms.style.left = newPos.x;
                    ms.style.top  = newPos.y + n.CardHeight;
                }
            }
            _canvas.MarkDirtyRepaint();
        }

        private void OnNodeDragged(StoryGraphNodeView node, Vector2 finalPos)
        {
            if (_dragStartPositions.Count > 1 && _dragStartPositions.TryGetValue(node, out var primaryStart))
            {
                // ── 다중 이동 저장 ──────────────────────
                Vector2 totalDelta = finalPos - primaryStart;

                var objs = new List<UnityEngine.Object>(_dragStartPositions.Count);
                foreach (var kvp in _dragStartPositions)
                    if (kvp.Key.Line != null) objs.Add(kvp.Key.Line);

                if (objs.Count > 0)
                    Undo.RecordObjects(objs.ToArray(), "Move Nodes");

                foreach (var kvp in _dragStartPositions)
                {
                    var n        = kvp.Key;
                    var nFinal   = new Vector2(
                        Mathf.Max(0f, kvp.Value.x + totalDelta.x),
                        Mathf.Max(0f, kvp.Value.y + totalDelta.y));

                    if (n.Line != null)
                    {
                        var so = new SerializedObject(n.Line);
                        so.FindProperty("editorNodePosition").vector2Value = nFinal;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(n.Line);
                    }

                    if (_nodeToModuleStack.TryGetValue(n, out var ms))
                    {
                        float w = n.resolvedStyle.width;
                        if (float.IsNaN(w) || w <= 0f) w = StoryGraphNodeView.DefaultW;
                        ms.style.left  = nFinal.x;
                        ms.style.top   = nFinal.y + n.CardHeight;
                        ms.style.width = w;
                    }
                }
            }
            else
            {
                // ── 단일 이동 저장 ──────────────────────
                StoryEditorUtility.SetEditorNodePosition(node.Line, finalPos, recordUndo: true);
                if (_nodeToModuleStack.TryGetValue(node, out var ms))
                {
                    float w = node.resolvedStyle.width;
                    if (float.IsNaN(w) || w <= 0f) w = StoryGraphNodeView.DefaultW;
                    ms.style.left  = finalPos.x;
                    ms.style.top   = finalPos.y + node.CardHeight;
                    ms.style.width = w;
                }
            }
            _canvas.MarkDirtyRepaint();
        }

        private void OnNodeResized(StoryGraphNodeView node, Vector2 size)
        {
            StoryEditorUtility.SetEditorNodeSize(node.Line, size, recordUndo: true);
            RepositionModuleStack(node);
            _canvas.MarkDirtyRepaint();
        }

        // ── 노드 클릭 핸들러 ─────────────────────────

        private void OnNodeClicked(StoryGraphNodeView node, bool isCtrl)
        {
            // connectable 포트 click-to-connect 완료
            if (_pendingConnectableStack != null && !_isConnectableDragConnecting)
            {
                _pendingConnectableStack.ApplyConnection(_pendingConnectableOptIdx, node.Line.LineId);
                ClearConnectablePending();
                return;
            }

            if (_pendingSource != null && !_isDragConnecting)
            {
                if (_pendingSource != node)
                    ConnectionMade?.Invoke(_pendingSource, node);
                ClearPending();
                return;
            }

            _selectedEdgeSrc               = null;
            _selectedConnectableEdgeStack  = null;
            _selectedConnectableEdgeOptIdx = -1;

            if (isCtrl)
            {
                // Ctrl+클릭: 토글 다중 선택
                if (_selectedNodes.Contains(node))
                    RemoveFromSelection(node);
                else
                    AddToSelection(node);
            }
            else
            {
                // 단일 선택
                SetSingleSelected(node);
            }
            FireSelectionChanged();
        }

        private void OnNodeDoubleClicked(StoryGraphNodeView node)
        {
            EditorGUIUtility.PingObject(node.Line);
            Selection.activeObject = node.Line;
        }

        // ── Connectable 포트 드래그 시작 ─────────────

        private void OnConnectablePortDragStart(StoryNodeModuleStackView stack, int slotIdx)
        {
            ClearPending();
            ClearConnectablePending();

            _pendingConnectableStack     = stack;
            _pendingConnectableOptIdx    = slotIdx;
            _isConnectableDragConnecting = false;
            _dragConnectCurrentPos       = stack.GetConnectablePortCanvasPos(slotIdx, _canvas);

            this.CapturePointer(PointerId.mousePointerId);
            _canvas.MarkDirtyRepaint();
        }

        private void ClearConnectablePending()
        {
            _pendingConnectableStack     = null;
            _pendingConnectableOptIdx    = 0;
            _isConnectableDragConnecting = false;
        }

        // ── 출력 포트 드래그 시작 ────────────────────

        private void OnOutputPortDragStart(StoryGraphNodeView node)
        {
            ClearPending();
            ClearConnectablePending();
            _pendingSource         = node;
            _isDragConnecting      = false;
            _dragConnectCurrentPos = node.OutPos;
            node.SetPendingSource(true);

            this.CapturePointer(PointerId.mousePointerId);
            _canvas.MarkDirtyRepaint();
        }

        // ── 캔버스 포인터 다운 (edge 선택 / marquee 시작) ──

        private void OnCanvasPointerDown(PointerDownEvent e)
        {
            if (e.button != 0) return;
            if (e.target != _canvas) return;

            Vector2 localPos = _canvas.WorldToLocal(e.position);

            // 일반 연결선 hit
            var hitSrc = FindEdgeAt(localPos);
            if (hitSrc != null)
            {
                _selectedEdgeSrc               = hitSrc;
                _selectedConnectableEdgeStack  = null;
                _selectedConnectableEdgeOptIdx = -1;
                ClearAllSelected();
                FireSelectionChanged();
                _canvas.MarkDirtyRepaint();
                e.StopPropagation();
                return;
            }

            // connectable 연결선 hit
            var (connectableStack, connectableOpt) = FindConnectableEdgeAt(localPos);
            if (connectableStack != null)
            {
                _selectedConnectableEdgeStack  = connectableStack;
                _selectedConnectableEdgeOptIdx = connectableOpt;
                _selectedEdgeSrc               = null;
                ClearAllSelected();
                FireSelectionChanged();
                _canvas.MarkDirtyRepaint();
                e.StopPropagation();
                return;
            }

            // 빈 공간 → 박스 선택 시작
            ClearPending();
            if (!e.ctrlKey)
            {
                ClearAllSelected();
                _selectedEdgeSrc               = null;
                _selectedConnectableEdgeStack  = null;
                _selectedConnectableEdgeOptIdx = -1;
            }

            _isMarqueeSelecting = true;
            _marqueeStart       = localPos;
            _marqueeEnd         = localPos;
            _marqueePointerId   = e.pointerId;

            if (!this.HasPointerCapture(PointerId.mousePointerId))
                this.CapturePointer(PointerId.mousePointerId);

            _canvas.MarkDirtyRepaint();
            e.StopPropagation();
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

        // ── 외부 포인터 이벤트 (pan + drag-connect + marquee) ──

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

            if (_isMarqueeSelecting)
            {
                _marqueeEnd = _canvas.WorldToLocal(e.position);
                _canvas.MarkDirtyRepaint();
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

            if (_pendingConnectableStack != null)
            {
                _dragConnectCurrentPos = _canvas.WorldToLocal(e.position);
                var portPos = _pendingConnectableStack.GetConnectablePortCanvasPos(_pendingConnectableOptIdx, _canvas);
                if (Vector2.Distance(_dragConnectCurrentPos, portPos) > DragConnectThresh)
                    _isConnectableDragConnecting = true;
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

            if (_isMarqueeSelecting && e.button == 0)
            {
                _isMarqueeSelecting = false;
                this.ReleasePointer(_marqueePointerId);
                _marqueePointerId = -1;
                FinishMarqueeSelection(e.ctrlKey);
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

            if (_pendingConnectableStack != null && e.button == 0)
            {
                this.ReleasePointer(e.pointerId);

                if (_isConnectableDragConnecting)
                {
                    Vector2 canvasPos = _canvas.WorldToLocal(e.position);
                    var target = FindNodeNearInputPort(canvasPos);
                    if (target != null)
                        _pendingConnectableStack.ApplyConnection(_pendingConnectableOptIdx, target.Line.LineId);
                    e.StopPropagation();
                }

                ClearConnectablePending();
                _canvas.MarkDirtyRepaint();
            }
        }

        private void CancelAllOuter()
        {
            if (_isPanning)               { _isPanning = false; }
            if (_isMarqueeSelecting)      { _isMarqueeSelecting = false; }
            if (_isDragConnecting)        { ClearPending();           _isDragConnecting            = false; }
            if (_isConnectableDragConnecting) { ClearConnectablePending(); }
        }

        // ── 박스 선택 완료 ────────────────────────────

        private void FinishMarqueeSelection(bool additive)
        {
            var rect = GetMarqueeRect();
            if (!additive) ClearAllSelected();

            // 드래그 거리가 거의 없으면 노드 선택 없이 종료
            if (rect is { width: < 4f, height: < 4f })
            {
                FireSelectionChanged();
                _canvas.MarkDirtyRepaint();
                return;
            }

            foreach (var n in _nodes)
            {
                float left   = n.resolvedStyle.left;
                float top    = n.resolvedStyle.top;
                float width  = n.resolvedStyle.width;
                float height = n.resolvedStyle.height;
                if (float.IsNaN(left) || float.IsNaN(top)) continue;
                if (float.IsNaN(width) || width <= 0f) width = StoryGraphNodeView.DefaultW;
                if (float.IsNaN(height) || height <= 0f) height = StoryGraphNodeView.DefaultH;

                var nodeRect = new Rect(left, top, width, height);
                if (rect.Overlaps(nodeRect))
                    AddToSelection(n);
            }

            FireSelectionChanged();
            _canvas.MarkDirtyRepaint();
        }

        private Rect GetMarqueeRect()
        {
            float x = Mathf.Min(_marqueeStart.x, _marqueeEnd.x);
            float y = Mathf.Min(_marqueeStart.y, _marqueeEnd.y);
            float w = Mathf.Abs(_marqueeEnd.x - _marqueeStart.x);
            float h = Mathf.Abs(_marqueeEnd.y - _marqueeStart.y);
            return new Rect(x, y, w, h);
        }

        // ── 선택 상태 헬퍼 ────────────────────────────

        private void SetSingleSelected(StoryGraphNodeView node)
        {
            ClearAllSelected();
            if (node != null)
            {
                _selectedNodes.Add(node);
                node.SetSelected(true);
                _primaryNode = node;
            }
        }

        private void AddToSelection(StoryGraphNodeView node)
        {
            if (_selectedNodes.Add(node))
            {
                node.SetSelected(true);
                _primaryNode ??= node;
            }
        }

        private void RemoveFromSelection(StoryGraphNodeView node)
        {
            if (_selectedNodes.Remove(node))
            {
                node.SetSelected(false);
                if (_primaryNode == node)
                {
                    _primaryNode = null;
                    foreach (var n in _selectedNodes) { _primaryNode = n; break; }
                }
            }
        }

        private void ClearAllSelected()
        {
            foreach (var n in _selectedNodes) n.SetSelected(false);
            _selectedNodes.Clear();
            _primaryNode = null;
        }

        private void FireSelectionChanged()
        {
            var list = new List<StoryGraphNodeView>(_selectedNodes);
            SelectionChanged?.Invoke(list);
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

        private void ClampPanOffset()
        {
            float viewW   = _viewport.resolvedStyle.width;
            float viewH   = _viewport.resolvedStyle.height;
            if (float.IsNaN(viewW) || viewW <= 0f || float.IsNaN(viewH) || viewH <= 0f) return;

            float canvasW = CanvasMinW * _zoomScale;
            float canvasH = CanvasMinH * _zoomScale;

            // canvas가 완전히 viewport 밖으로 나가지 않도록 (margin만큼은 항상 보임)
            float minX = -(canvasW - PanMargin);
            float maxX =   viewW   - PanMargin;
            float minY = -(canvasH - PanMargin);
            float maxY =   viewH   - PanMargin;

            _panOffset.x = Mathf.Clamp(_panOffset.x, minX, maxX);
            _panOffset.y = Mathf.Clamp(_panOffset.y, minY, maxY);
        }

        private void ApplyTransform()
        {
            ClampPanOffset();
            _canvas.style.translate = new Vector3(_panOffset.x, _panOffset.y, 0f);
            _canvas.style.scale    = new Vector3(_zoomScale, _zoomScale, 1f);
            _canvas.MarkDirtyRepaint();
            _viewport.MarkDirtyRepaint();   // 그리드 재드로우
        }

        private void UpdateNodeZoom()
        {
            foreach (var n in _nodes) n.ZoomScale = _zoomScale;
        }

        // ── 배경 그리드 드로잉 ───────────────────────

        private void DrawBackground(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            float w = _viewport.resolvedStyle.width;
            float h = _viewport.resolvedStyle.height;
            if (float.IsNaN(w) || w <= 0f || float.IsNaN(h) || h <= 0f) return;

            // ── 배경 fill ────────────────────────────
            p.fillColor = new Color(0.155f, 0.155f, 0.155f);
            p.BeginPath();
            p.MoveTo(Vector2.zero);
            p.LineTo(new Vector2(w, 0f));
            p.LineTo(new Vector2(w, h));
            p.LineTo(new Vector2(0f, h));
            p.ClosePath();
            p.Fill();

            // ── 마이너 그리드 ─────────────────────────
            float minorPx = GridMinor * _zoomScale;
            if (minorPx >= GridMinorHideBelow)
            {
                float ox = ((_panOffset.x % minorPx) + minorPx) % minorPx;
                float oy = ((_panOffset.y % minorPx) + minorPx) % minorPx;

                p.strokeColor = new Color(1f, 1f, 1f, 0.035f);
                p.lineWidth   = 1f;

                p.BeginPath();
                for (float x = ox; x <= w; x += minorPx)
                {
                    p.MoveTo(new Vector2(x, 0f));
                    p.LineTo(new Vector2(x, h));
                }
                for (float y = oy; y <= h; y += minorPx)
                {
                    p.MoveTo(new Vector2(0f, y));
                    p.LineTo(new Vector2(w, y));
                }
                p.Stroke();
            }

            // ── 메이저 그리드 ─────────────────────────
            float majorPx = GridMajor * _zoomScale;
            {
                float ox = ((_panOffset.x % majorPx) + majorPx) % majorPx;
                float oy = ((_panOffset.y % majorPx) + majorPx) % majorPx;

                p.strokeColor = new Color(1f, 1f, 1f, 0.085f);
                p.lineWidth   = 1f;

                p.BeginPath();
                for (float x = ox; x <= w; x += majorPx)
                {
                    p.MoveTo(new Vector2(x, 0f));
                    p.LineTo(new Vector2(x, h));
                }
                for (float y = oy; y <= h; y += majorPx)
                {
                    p.MoveTo(new Vector2(0f, y));
                    p.LineTo(new Vector2(w, y));
                }
                p.Stroke();
            }
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
                    selected ? new Color(1f, 0.8f, 0.2f, 0.95f) 
                        : new Color(0.4f, 0.8f, 0.4f, 0.85f),
                    selected ? 3f : 2f);
            }

            // connectable 포트 → 라인 노드 연결선
            foreach (var ms in _moduleStacks)
            {
                int portCount = ms.ConnectablePortCount;
                for (int i = 0; i < portCount; i++)
                {
                    if (_isConnectableDragConnecting && ms == _pendingConnectableStack && i == _pendingConnectableOptIdx)
                        continue;

                    string targetId = ms.GetConnectablePortConnection(i);
                    if (string.IsNullOrWhiteSpace(targetId)) continue;

                    var dst = FindNodeById(targetId);
                    if (dst == null) continue;

                    Vector2 from = ms.GetConnectablePortCanvasPos(i, _canvas);
                    if (from == Vector2.zero) continue;

                    bool isSelected = ms == _selectedConnectableEdgeStack && i == _selectedConnectableEdgeOptIdx;
                    DrawBezier(p, from, dst.InPos,
                        isSelected ? new Color(1f, 0.9f, 0.2f, 0.95f) 
                            : new Color(1f, 0.78f, 0.3f, 0.85f),
                        isSelected ? 3f : 2f);
                }
            }

            // 드래그 preview 선 (라인 노드 포트)
            if (_pendingSource != null && _isDragConnecting && _dragConnectCurrentPos != Vector2.zero)
            {
                DrawBezier(p, _pendingSource.OutPos, _dragConnectCurrentPos,
                    new Color(0.9f, 0.9f, 0.3f, 0.7f), 1.5f);
            }

            // 드래그 preview 선 (connectable 포트)
            if (_pendingConnectableStack != null && _isConnectableDragConnecting && _dragConnectCurrentPos != Vector2.zero)
            {
                Vector2 from = _pendingConnectableStack.GetConnectablePortCanvasPos(_pendingConnectableOptIdx, _canvas);
                if (from != Vector2.zero)
                    DrawBezier(p, from, _dragConnectCurrentPos, 
                        new Color(1f, 0.78f, 0.3f, 0.7f), 1.5f);
            }

            // 박스 선택 사각형
            if (_isMarqueeSelecting)
            {
                var r = GetMarqueeRect();
                if (r.width > 1f || r.height > 1f)
                {
                    // fill
                    p.fillColor = new Color(0.35f, 0.65f, 1f, 0.08f);
                    p.BeginPath();
                    p.MoveTo(new Vector2(r.x,    r.y));
                    p.LineTo(new Vector2(r.xMax, r.y));
                    p.LineTo(new Vector2(r.xMax, r.yMax));
                    p.LineTo(new Vector2(r.x,    r.yMax));
                    p.ClosePath();
                    p.Fill();

                    // stroke
                    p.strokeColor = new Color(0.45f, 0.75f, 1f, 0.85f);
                    p.lineWidth   = 1.5f;
                    p.BeginPath();
                    p.MoveTo(new Vector2(r.x,    r.y));
                    p.LineTo(new Vector2(r.xMax, r.y));
                    p.LineTo(new Vector2(r.xMax, r.yMax));
                    p.LineTo(new Vector2(r.x,    r.yMax));
                    p.ClosePath();
                    p.Stroke();
                }
            }
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

        private (StoryNodeModuleStackView stack, int optIdx) FindConnectableEdgeAt(Vector2 canvasPos)
        {
            foreach (var ms in _moduleStacks)
            {
                int portCount = ms.ConnectablePortCount;
                for (int i = 0; i < portCount; i++)
                {
                    string targetId = ms.GetConnectablePortConnection(i);
                    if (string.IsNullOrWhiteSpace(targetId)) continue;
                    var dst = FindNodeById(targetId);
                    if (dst == null) continue;
                    Vector2 from = ms.GetConnectablePortCanvasPos(i, _canvas);
                    if (from == Vector2.zero) continue;
                    if (IsNearBezier(from, dst.InPos, canvasPos, EdgeHitThreshold))
                        return (ms, i);
                }
            }
            return (null, -1);
        }

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
    }
}
