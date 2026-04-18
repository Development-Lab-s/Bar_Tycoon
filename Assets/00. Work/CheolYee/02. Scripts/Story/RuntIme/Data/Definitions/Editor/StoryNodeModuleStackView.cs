using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Modules;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 캔버스에서 line node 카드 바로 아래에 배치되는 모듈 스택 뷰.
    ///
    /// - 인라인 모듈: IMGUI로 generic PropertyField 편집 (타입 분기 없음)
    /// - Connectable 포트: IStoryGraphConnectableModule capability 를 가진 모든 모듈
    ///   (SO modules + inline modules 모두 처리) 의 포트를 포트 스트립에 통합 노출
    ///
    /// Canvas 에서는 ConnectablePortDragStart 이벤트와
    /// GetConnectablePortCanvasPos / GetConnectablePortConnection / ApplyConnection / ApplyDisconnect
    /// 를 통해 연결선을 관리합니다.
    /// </summary>
    public sealed class StoryNodeModuleStackView : VisualElement
    {
        /// <summary>모듈 추가/삭제 후 캔버스 갱신을 요청한다.</summary>
        public event Action Changed;

        /// <summary>connectable 포트 PointerDown → canvas 가 드래그 연결 시작. 두 번째 인수는 슬롯 인덱스.</summary>
        public event Action<StoryNodeModuleStackView, int> ConnectablePortDragStart;

        // ── 내부 상태 ────────────────────────────────
        private readonly StoryLineSO           _line;
        private          SerializedObject       _so;
        private readonly Dictionary<int, bool> _expandedState = new();

        private Rect _addBtnRect;
        private bool _pendingRefresh;

        // ── 포트 슬롯 ─────────────────────────────────
        // 각 슬롯은 자신의 읽기/쓰기/연결해제를 캡처된 SerializedObject 경로로 처리한다.
        private sealed class PortSlot
        {
            public VisualElement  Element;
            public string         Label;
            public Func<string>   GetConnection;   // 현재 targetLineId 읽기
            public Action<string> SetConnection;   // SerializedObject 통해 씀 (Undo 포함)
            public Action         Disconnect;      // SerializedObject 통해 비움 (Undo 포함)
        }

        private readonly List<PortSlot> _portSlots = new();
        private const float PortR = 7f;

        // ── 생성자 ───────────────────────────────────

        public StoryNodeModuleStackView(StoryLineSO line)
        {
            _line = line;

            style.position            = Position.Absolute;
            style.borderBottomLeftRadius  = 4;
            style.borderBottomRightRadius = 4;
            style.borderLeftWidth     = 1;
            style.borderLeftColor     = new StyleColor(new Color(0.35f, 0.35f, 0.35f));
            style.borderRightWidth    = 1;
            style.borderRightColor    = new StyleColor(new Color(0.35f, 0.35f, 0.35f));
            style.borderBottomWidth   = 1;
            style.borderBottomColor   = new StyleColor(new Color(0.35f, 0.35f, 0.35f));
            style.borderTopWidth      = 2;
            style.borderTopColor      = new StyleColor(new Color(0.22f, 0.38f, 0.60f));
            style.backgroundColor     = new StyleColor(new Color(0.19f, 0.19f, 0.21f));

            Refresh();
        }

        // ── 공개 API ─────────────────────────────────

        public StoryLineSO Line => _line;

        /// <summary>현재 노출된 connectable 포트 총 수 (SO + inline 합산).</summary>
        public int ConnectablePortCount => _portSlots.Count;

        public void Refresh()
        {
            _so = _line != null ? new SerializedObject(_line) : null;
            Rebuild();
            style.display = _line != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>슬롯 인덱스 기준 포트의 canvas-local 중심 좌표를 반환한다.</summary>
        public Vector2 GetConnectablePortCanvasPos(int slotIdx, VisualElement canvas)
        {
            if ((uint)slotIdx >= (uint)_portSlots.Count) return Vector2.zero;
            var port   = _portSlots[slotIdx].Element;
            var center = port.contentRect.center;
            if (float.IsNaN(center.x) || float.IsNaN(center.y)) return Vector2.zero;
            return canvas.WorldToLocal(port.LocalToWorld(center));
        }

        /// <summary>슬롯 인덱스 기준 현재 연결된 targetLineId 를 반환한다.</summary>
        public string GetConnectablePortConnection(int slotIdx)
        {
            if ((uint)slotIdx >= (uint)_portSlots.Count) return null;
            return _portSlots[slotIdx].GetConnection?.Invoke();
        }

        /// <summary>슬롯에 연결을 적용한다 (SerializedObject 통해 Undo 지원).</summary>
        public void ApplyConnection(int slotIdx, string targetLineId)
        {
            if ((uint)slotIdx >= (uint)_portSlots.Count) return;
            _portSlots[slotIdx].SetConnection?.Invoke(targetLineId);
            Refresh();
            Changed?.Invoke();
        }

        /// <summary>슬롯 연결을 해제한다 (SerializedObject 통해 Undo 지원).</summary>
        public void ApplyDisconnect(int slotIdx)
        {
            if ((uint)slotIdx >= (uint)_portSlots.Count) return;
            _portSlots[slotIdx].Disconnect?.Invoke();
            Refresh();
            Changed?.Invoke();
        }

        // ── 내부 구성 ─────────────────────────────────

        private void Rebuild()
        {
            Clear();
            _portSlots.Clear();

            // ① 인라인 모듈 편집 IMGUI
            var modulesImgui = new IMGUIContainer(DrawModulesIMGUI);
            modulesImgui.style.paddingLeft   = 4;
            modulesImgui.style.paddingRight  = 4;
            modulesImgui.style.paddingTop    = 2;
            modulesImgui.style.paddingBottom = 2;
            Add(modulesImgui);

            // ② Connectable 포트 스트립 (SO + inline 모두 처리)
            BuildConnectablePortStrip();

            // ③ + Module 버튼 IMGUI
            var addImgui = new IMGUIContainer(DrawAddButtonIMGUI);
            addImgui.style.paddingLeft   = 4;
            addImgui.style.paddingRight  = 4;
            addImgui.style.paddingTop    = 1;
            addImgui.style.paddingBottom = 3;
            Add(addImgui);
        }

        // ── IMGUI — 인라인 모듈 블록 (generic PropertyField) ───────────────

        private void DrawModulesIMGUI()
        {
            if (_line == null || _so == null) return;
            _so.Update();

            var inlineProp = _so.FindProperty("inlineModules");
            if (inlineProp == null || inlineProp.arraySize == 0)
            {
                _so.ApplyModifiedProperties();
                return;
            }

            int toDelete = -1;

            for (int i = 0; i < inlineProp.arraySize; i++)
            {
                var    elem  = inlineProp.GetArrayElementAtIndex(i);
                var    data  = elem.managedReferenceValue as StoryInlineModuleData;
                string label = data?.DisplayName ?? $"?#{i}";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                if (!_expandedState.ContainsKey(i)) _expandedState[i] = false;

                // 강조색은 attribute 기반 (타입 분기 없음)
                var prevContent = GUI.contentColor;
                GUI.contentColor = StoryModuleMetadataAttribute.GetColor(data?.GetType());
                _expandedState[i] = EditorGUILayout.Foldout(_expandedState[i], label, true);
                GUI.contentColor  = prevContent;

                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.85f, 0.3f, 0.3f);
                if (GUILayout.Button("✕", GUILayout.Width(20),
                        GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    toDelete = i;
                GUI.backgroundColor = prevBg;

                EditorGUILayout.EndHorizontal();

                if (_expandedState[i])
                {
                    // generic PropertyField 렌더: 타입별 분기 없음
                    EditorGUI.indentLevel++;
                    var child = elem.Copy();
                    var end   = elem.GetEndProperty();
                    bool first = true;
                    while (child.NextVisible(first))
                    {
                        first = false;
                        if (SerializedProperty.EqualContents(child, end)) break;
                        EditorGUILayout.PropertyField(child, true);
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(1);
            }

            _so.ApplyModifiedProperties();
            CheckConnectablePortCountChange();

            if (toDelete >= 0)
            {
                Undo.RecordObject(_line, "Remove Inline Module");
                _so.Update();
                var prop = _so.FindProperty("inlineModules");
                if (toDelete < prop.arraySize)
                {
                    prop.DeleteArrayElementAtIndex(toDelete);
                    _so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_line);
                }
                _expandedState.Clear();
                Refresh();
                Changed?.Invoke();
            }
        }

        // ── IMGUI — + Module 버튼 ─────────────────────

        private void DrawAddButtonIMGUI()
        {
            var prevBtnBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.25f, 0.40f, 0.65f);
            bool addClicked = GUILayout.Button("+ Module", GUILayout.Height(18));
            GUI.backgroundColor = prevBtnBg;

            if (Event.current.type == EventType.Repaint)
                _addBtnRect = GUILayoutUtility.GetLastRect();

            if (addClicked)
            {
                var screenPt = GUIUtility.GUIToScreenPoint(
                    new Vector2(_addBtnRect.x, _addBtnRect.yMax));
                PopupWindow.Show(
                    new Rect(screenPt, new Vector2(_addBtnRect.width, 0)),
                    new StoryModuleSearchPopup(AddModuleToLine));
            }
        }

        // ── 모듈 추가 ─────────────────────────────────

        private void AddModuleToLine(Type type)
        {
            if (_line == null) return;

            Undo.RecordObject(_line, $"Add Inline Module: {type.Name}");
            _so = new SerializedObject(_line);
            _so.Update();

            var prop = _so.FindProperty("inlineModules");
            prop.arraySize++;
            prop.GetArrayElementAtIndex(prop.arraySize - 1).managedReferenceValue =
                Activator.CreateInstance(type);

            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_line);

            _expandedState[prop.arraySize - 1] = true;
            Refresh();
            Changed?.Invoke();
        }

        // ── Connectable 포트 스트립 ────────────────────

        private void BuildConnectablePortStrip()
        {
            if (_line == null) return;

            // SO modules + inline modules 에서 IStoryGraphConnectableModule 수집
            bool anyConnectable = false;

            foreach (var soModule in _line.Modules)
            {
                if (soModule is IStoryGraphConnectableModule)
                {
                    anyConnectable = true;
                    break;
                }
            }

            if (!anyConnectable)
            {
                foreach (var inlineMod in _line.InlineModules)
                {
                    if (inlineMod is IStoryGraphConnectableModule)
                    {
                        anyConnectable = true;
                        break;
                    }
                }
            }

            if (!anyConnectable) return;

            var strip = new VisualElement();
            strip.style.borderTopWidth  = 1;
            strip.style.borderTopColor  = new StyleColor(new Color(1f, 0.78f, 0.3f, 0.35f));
            strip.style.paddingTop      = 3;
            strip.style.paddingBottom   = 3;
            strip.style.backgroundColor = new StyleColor(new Color(0.17f, 0.17f, 0.19f));

            int totalPorts = CountTotalConnectablePorts();
            var headerLbl = new Label($"▸ Connectable Ports  ({totalPorts} ports)");
            headerLbl.style.fontSize      = 9;
            headerLbl.style.paddingLeft   = 6;
            headerLbl.style.paddingBottom = 1;
            headerLbl.style.color         = new StyleColor(new Color(1f, 0.78f, 0.3f, 0.75f));
            strip.Add(headerLbl);

            // SO modules
            foreach (var soModule in _line.Modules)
            {
                if (soModule is not IStoryGraphConnectableModule connectable) continue;
                var ports = connectable.GetPorts();
                for (int p = 0; p < ports.Count; p++)
                {
                    var capturedSO   = soModule;
                    int capturedPort = p;
                    string portLabel = ports[p].Label;

                    var slot = new PortSlot
                    {
                        Label         = portLabel,
                        GetConnection = () => (capturedSO as IStoryGraphConnectableModule)?.GetPortConnection(capturedPort),
                        SetConnection = targetLineId =>
                        {
                            var so = new SerializedObject(capturedSO);
                            so.Update();
                            Undo.RecordObject(capturedSO, "Connect Module Port");
                            var optsProp = so.FindProperty("options");
                            if (optsProp != null && capturedPort < optsProp.arraySize)
                                optsProp.GetArrayElementAtIndex(capturedPort)
                                        .FindPropertyRelative("reactionStartLineId").stringValue = targetLineId;
                            so.ApplyModifiedProperties();
                            EditorUtility.SetDirty(capturedSO);
                        },
                        Disconnect = () =>
                        {
                            var so = new SerializedObject(capturedSO);
                            so.Update();
                            Undo.RecordObject(capturedSO, "Disconnect Module Port");
                            var optsProp = so.FindProperty("options");
                            if (optsProp != null && capturedPort < optsProp.arraySize)
                                optsProp.GetArrayElementAtIndex(capturedPort)
                                        .FindPropertyRelative("reactionStartLineId").stringValue = "";
                            so.ApplyModifiedProperties();
                            EditorUtility.SetDirty(capturedSO);
                        },
                    };

                    int slotIdx = _portSlots.Count;
                    slot.Element = BuildPortRow(strip, slot, slotIdx);
                    _portSlots.Add(slot);
                }
            }

            // Inline modules
            for (int m = 0; m < _line.InlineModules.Count; m++)
            {
                if (_line.InlineModules[m] is not IStoryGraphConnectableModule connectable) continue;
                var ports = connectable.GetPorts();
                for (int p = 0; p < ports.Count; p++)
                {
                    int capturedM    = m;
                    int capturedPort = p;
                    string portLabel = ports[p].Label;

                    var slot = new PortSlot
                    {
                        Label         = portLabel,
                        GetConnection = () =>
                        {
                            if ((uint)capturedM >= (uint)_line.InlineModules.Count) return null;
                            return (_line.InlineModules[capturedM] as IStoryGraphConnectableModule)?.GetPortConnection(capturedPort);
                        },
                        SetConnection = targetLineId =>
                        {
                            var so = new SerializedObject(_line);
                            so.Update();
                            Undo.RecordObject(_line, "Connect Module Port");
                            var inlineProp = so.FindProperty("inlineModules");
                            var optsProp   = inlineProp.GetArrayElementAtIndex(capturedM).FindPropertyRelative("options");
                            if (optsProp != null && capturedPort < optsProp.arraySize)
                                optsProp.GetArrayElementAtIndex(capturedPort)
                                        .FindPropertyRelative("reactionStartLineId").stringValue = targetLineId;
                            so.ApplyModifiedProperties();
                            EditorUtility.SetDirty(_line);
                        },
                        Disconnect = () =>
                        {
                            var so = new SerializedObject(_line);
                            so.Update();
                            Undo.RecordObject(_line, "Disconnect Module Port");
                            var inlineProp = so.FindProperty("inlineModules");
                            var optsProp   = inlineProp.GetArrayElementAtIndex(capturedM).FindPropertyRelative("options");
                            if (optsProp != null && capturedPort < optsProp.arraySize)
                                optsProp.GetArrayElementAtIndex(capturedPort)
                                        .FindPropertyRelative("reactionStartLineId").stringValue = "";
                            so.ApplyModifiedProperties();
                            EditorUtility.SetDirty(_line);
                        },
                    };

                    int slotIdx = _portSlots.Count;
                    slot.Element = BuildPortRow(strip, slot, slotIdx);
                    _portSlots.Add(slot);
                }
            }

            Add(strip);
        }

        private VisualElement BuildPortRow(VisualElement parent, PortSlot slot, int slotIdx)
        {
            string connection = slot.GetConnection?.Invoke();
            bool   connected  = !string.IsNullOrWhiteSpace(connection);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;
            row.style.height        = 22;
            row.style.paddingLeft   = 6;
            row.style.paddingRight  = 6;

            string labelText = string.IsNullOrWhiteSpace(slot.Label)
                ? $"Port {slotIdx}" : Trunc(slot.Label, 22);
            var lbl = new Label(labelText);
            lbl.style.flexGrow = 1;
            lbl.style.fontSize = 10;
            lbl.style.overflow = Overflow.Hidden;
            lbl.style.color    = new StyleColor(connected
                ? new Color(0.45f, 0.95f, 0.5f)
                : new Color(0.72f, 0.72f, 0.72f));
            row.Add(lbl);

            if (connected)
            {
                var idLbl = new Label($"→ {Trunc(connection, 14)}");
                idLbl.style.fontSize    = 9;
                idLbl.style.color       = new StyleColor(new Color(0.45f, 0.9f, 0.5f, 0.75f));
                idLbl.style.marginRight = 4;
                row.Add(idLbl);
            }

            var port = new VisualElement();
            port.style.width  = PortR * 2;
            port.style.height = PortR * 2;
            port.style.borderTopLeftRadius    = port.style.borderTopRightRadius    =
            port.style.borderBottomLeftRadius = port.style.borderBottomRightRadius = PortR;
            port.style.backgroundColor = new StyleColor(connected
                ? new Color(0.3f, 0.85f, 0.4f)
                : new Color(1f, 0.78f, 0.3f));
            port.style.flexShrink = 0;
            port.tooltip          = connected
                ? $"연결됨: {connection}\n우클릭 → 연결 해제"
                : "드래그 또는 클릭 → 라인 연결";

            int capturedSlot = slotIdx;
            port.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                ConnectablePortDragStart?.Invoke(this, capturedSlot);
                e.StopPropagation();
            });
            port.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                bool has = !string.IsNullOrWhiteSpace(_portSlots[capturedSlot].GetConnection?.Invoke());
                evt.menu.AppendAction("Disconnect",
                    _ => ApplyDisconnect(capturedSlot),
                    has ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }));

            row.Add(port);
            parent.Add(row);
            return port;
        }

        // ── 포트 수 변화 감지 ──────────────────────────

        private void CheckConnectablePortCountChange()
        {
            if (_pendingRefresh || _so == null) return;
            int current = CountTotalConnectablePorts();
            if (current == _portSlots.Count) return;

            _pendingRefresh = true;
            EditorApplication.delayCall += () =>
            {
                _pendingRefresh = false;
                Refresh();
                Changed?.Invoke();
            };
        }

        private int CountTotalConnectablePorts()
        {
            int count = 0;
            if (_line == null) return 0;
            foreach (var m in _line.Modules)
                if (m is IStoryGraphConnectableModule c) count += c.GetPorts().Count;
            foreach (var m in _line.InlineModules)
                if (m is IStoryGraphConnectableModule c) count += c.GetPorts().Count;
            return count;
        }

        // ── 헬퍼 ─────────────────────────────────────

        private static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace('\n', ' ');
            return s.Length <= max ? s : s[..max] + "…";
        }
    }
}
