using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 노드 박스 + 연결선을 그리는 캔버스 VisualElement.
    /// StoryGraphEditorWindow가 소유하며 좌측 패널 전체를 차지한다.
    /// </summary>
    public sealed class StoryGraphCanvasView : VisualElement
    {
        // ── 레이아웃 상수 ────────────────────────────
        private const float StartX   = 40f;
        private const float StartY   = 30f;
        private const float StepY    = 110f;

        // ── 이벤트 ───────────────────────────────────
        /// <summary>출력 포트 → 다른 노드 클릭으로 연결이 확정됐을 때. (source, target)</summary>
        public event Action<StoryGraphNodeView, StoryGraphNodeView> ConnectionMade;
        /// <summary>노드 단순 클릭. 우측 디테일 패널 갱신에 사용.</summary>
        public event Action<StoryGraphNodeView> NodeSelected;

        // ── 상태 ─────────────────────────────────────
        private readonly VisualElement _canvas;   // 실제 노드/라인을 품는 컨테이너
        private readonly List<StoryGraphNodeView> _nodes = new();
        private StoryGraphNodeView _selectedNode;
        private StoryGraphNodeView _pendingSource; // 연결 중인 출력 포트 소유 노드

        // ── 생성자 ───────────────────────────────────
        public StoryGraphCanvasView()
        {
            style.flexGrow = 1;

            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.style.flexGrow = 1;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            scroll.verticalScrollerVisibility   = ScrollerVisibility.Auto;

            _canvas = new VisualElement();
            _canvas.style.position = Position.Relative;
            _canvas.style.minWidth  = 800;
            _canvas.style.minHeight = 600;
            _canvas.generateVisualContent += DrawConnections;

            // 빈 공간 클릭 → 연결 취소
            _canvas.RegisterCallback<ClickEvent>(OnCanvasClicked);

            scroll.Add(_canvas);
            Add(scroll);
        }

        // ── 공개 API ─────────────────────────────────

        /// <summary>에피소드 Lines 목록으로 캔버스를 완전히 재구성한다.</summary>
        public void Rebuild(IReadOnlyList<StoryLineSO> lines)
        {
            _canvas.Clear();
            _nodes.Clear();
            _selectedNode  = null;
            _pendingSource = null;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == null) continue;
                var node = CreateNode(lines[i]);

                // 저장된 위치가 있으면 복원, 없으면 자동 배치
                if (lines[i].HasEditorNodePosition)
                {
                    Vector2 saved = lines[i].EditorNodePosition;
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
            }

            ExpandCanvasHeight(lines.Count);
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>특定 라인을 선택 상태로 만든다 (외부에서 ListView 연동 시 사용).</summary>
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

        /// <summary>노드 내용을 갱신한다 (lineId 변경 후 호출).</summary>
        public void RefreshAll()
        {
            foreach (var n in _nodes) n.Refresh();
            _canvas.MarkDirtyRepaint();
        }

        public StoryGraphNodeView SelectedNode => _selectedNode;

        // ── 노드 생성 ────────────────────────────────

        private StoryGraphNodeView CreateNode(StoryLineSO line)
        {
            var node = new StoryGraphNodeView(line);

            node.IsConnectModeActive = () => _pendingSource != null;
            node.Clicked             += OnNodeClicked;
            node.DoubleClicked       += OnNodeDoubleClicked;
            node.OutputPortClicked   += OnOutputPortClicked;
            node.Dragged             += OnNodeDragged;

            // 레이아웃 변경 시 연결선 재렌더
            node.RegisterCallback<GeometryChangedEvent>(_ => _canvas.MarkDirtyRepaint());

            return node;
        }

        private void OnNodeDragged(StoryGraphNodeView node, Vector2 pos)
        {
            StoryEditorUtility.SetEditorNodePosition(node.Line, pos);
            _canvas.MarkDirtyRepaint();
        }

        // ── 클릭 핸들러 ──────────────────────────────

        private void OnNodeClicked(StoryGraphNodeView node)
        {
            if (_pendingSource != null)
            {
                // 연결 확정
                if (_pendingSource != node)                       // 자기 자신 연결 금지
                    ConnectionMade?.Invoke(_pendingSource, node);

                ClearPending();
                return;
            }

            SetSelected(node);
            NodeSelected?.Invoke(node);
        }

        private void OnNodeDoubleClicked(StoryGraphNodeView node)
        {
            // 더블클릭 → Project 창에서 asset ping
            EditorGUIUtility.PingObject(node.Line);
            Selection.activeObject = node.Line;
        }

        private void OnOutputPortClicked(StoryGraphNodeView node)
        {
            if (_pendingSource == node)
            {
                // 이미 선택된 소스를 다시 클릭 → 취소
                ClearPending();
                return;
            }
            ClearPending();
            _pendingSource = node;
            node.SetPendingSource(true);
        }

        private void OnCanvasClicked(ClickEvent e)
        {
            if (e.target != _canvas) return;
            ClearPending();
            SetSelected(null);
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
            _canvas.MarkDirtyRepaint();
        }

        // ── 연결선 드로잉 ────────────────────────────

        private void DrawConnections(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;

            foreach (var src in _nodes)
            {
                string nextId = src.Line?.NextLineId;
                if (string.IsNullOrWhiteSpace(nextId)) continue;

                StoryGraphNodeView dst = FindNodeById(nextId);
                if (dst == null) continue;

                DrawBezier(painter, src.OutPos, dst.InPos, new Color(0.4f, 0.8f, 0.4f, 0.85f));
            }

            // 연결 중 미리보기선 (pending → 마우스 위치는 알 수 없으므로 dst=null일 때 생략)
        }

        private void DrawBezier(Painter2D p, Vector2 from, Vector2 to, Color color)
        {
            float dx     = Mathf.Abs(to.x - from.x);
            float tanLen = Mathf.Max(dx * 0.5f, 60f);

            var c1 = new Vector2(from.x + tanLen, from.y);
            var c2 = new Vector2(to.x   - tanLen, to.y);

            p.strokeColor = color;
            p.lineWidth   = 2f;
            p.BeginPath();
            p.MoveTo(from);
            p.BezierCurveTo(c1, c2, to);
            p.Stroke();
        }

        private StoryGraphNodeView FindNodeById(string lineId)
        {
            foreach (var n in _nodes)
                if (n.Line != null && n.Line.LineId == lineId) return n;
            return null;
        }

        private void ExpandCanvasHeight(int nodeCount)
        {
            float needed = StartY + nodeCount * StepY + 80f;
            _canvas.style.minHeight = Mathf.Max(600f, needed);
        }
    }
}
