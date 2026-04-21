using System;
using System.Collections.Generic;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Attributes;
using _00._Work.CheolYee._02._Scripts.Story.RuntIme.Shared.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace _00._Work.CheolYee._02._Scripts.Story.RuntIme.Data.Definitions.Editor
{
    /// <summary>
    /// 캔버스에서 line node 카드 바로 아래에 배치되는 모듈 스택 뷰.
    ///
    /// - SO 모듈: IMGUI로 SerializedObject 기반 편집 (타입 분기 없음)
    /// - Connectable 포트: IStoryGraphConnectableModule capability 를 가진 SO 모듈의
    ///   포트를 포트 스트립에 통합 노출
    /// - Add Module: StoryModuleSO sub-asset 생성 → modules 리스트 추가까지 원스텝
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
        private sealed class PortSlot
        {
            public VisualElement  element;
            public string         label;
            public Func<string>   getConnection;
            public Action<string> setConnection;
            public Action         disconnect;
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

        /// <summary>현재 노출된 connectable 포트 총 수.</summary>
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
            var port   = _portSlots[slotIdx].element;
            var center = port.contentRect.center;
            if (float.IsNaN(center.x) || float.IsNaN(center.y)) return Vector2.zero;
            return canvas.WorldToLocal(port.LocalToWorld(center));
        }

        /// <summary>슬롯 인덱스 기준 현재 연결된 targetLineId 를 반환한다.</summary>
        public string GetConnectablePortConnection(int slotIdx)
        {
            if ((uint)slotIdx >= (uint)_portSlots.Count) return null;
            return _portSlots[slotIdx].getConnection?.Invoke();
        }

        /// <summary>슬롯에 연결을 적용한다 (SerializedObject 통해 Undo 지원).</summary>
        public void ApplyConnection(int slotIdx, string targetLineId)
        {
            if ((uint)slotIdx >= (uint)_portSlots.Count) return;
            _portSlots[slotIdx].setConnection?.Invoke(targetLineId);
            Refresh();
            Changed?.Invoke();
        }

        /// <summary>슬롯 연결을 해제한다 (SerializedObject 통해 Undo 지원).</summary>
        public void ApplyDisconnect(int slotIdx)
        {
            if ((uint)slotIdx >= (uint)_portSlots.Count) return;
            _portSlots[slotIdx].disconnect?.Invoke();
            Refresh();
            Changed?.Invoke();
        }

        // ── 내부 구성 ─────────────────────────────────

        private void Rebuild()
        {
            Clear();
            _portSlots.Clear();

            // ① SO 모듈 편집 IMGUI
            var modulesImgui = new IMGUIContainer(DrawModulesIMGUI);
            modulesImgui.style.paddingLeft   = 4;
            modulesImgui.style.paddingRight  = 4;
            modulesImgui.style.paddingTop    = 2;
            modulesImgui.style.paddingBottom = 2;
            Add(modulesImgui);

            // ② Connectable 포트 스트립 (SO modules)
            BuildConnectablePortStrip();

            // ③ + Module 버튼 IMGUI
            var addImgui = new IMGUIContainer(DrawAddButtonIMGUI);
            addImgui.style.paddingLeft   = 4;
            addImgui.style.paddingRight  = 4;
            addImgui.style.paddingTop    = 1;
            addImgui.style.paddingBottom = 3;
            Add(addImgui);
        }

        // ── IMGUI — SO 모듈 블록 ────────────────────────────────────────────

        private void DrawModulesIMGUI()
        {
            if (_line == null || _so == null) return;
            _so.Update();

            var modulesProp = _so.FindProperty("modules");
            if (modulesProp == null || modulesProp.arraySize == 0)
            {
                _so.ApplyModifiedProperties();
                return;
            }

            int toDelete = -1;

            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                var elemProp = modulesProp.GetArrayElementAtIndex(i);
                var module   = elemProp.objectReferenceValue as StoryModuleSO;
                if (module == null) continue;

                string label = module.DisplayName;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                _expandedState.TryAdd(i, false);

                var prevContent = GUI.contentColor;
                GUI.contentColor = StoryModuleMetadataAttribute.GetColor(module.GetType());
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
                    EditorGUI.indentLevel++;
                    var moduleSO = new SerializedObject(module);
                    moduleSO.Update();
                    var iter = moduleSO.GetIterator();
                    bool enterChildren = true;
                    while (iter.NextVisible(enterChildren))
                    {
                        enterChildren = false;
                        if (iter.name == "m_Script") continue;
                        EditorGUILayout.PropertyField(iter, true);
                    }
                    if (moduleSO.ApplyModifiedProperties())
                        EditorUtility.SetDirty(module);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(1);
            }

            _so.ApplyModifiedProperties();
            CheckConnectablePortCountChange();

            if (toDelete >= 0)
            {
                _so.Update();
                var prop      = _so.FindProperty("modules");
                var objToRemove = prop.GetArrayElementAtIndex(toDelete).objectReferenceValue;

                // ObjectReference 배열: null 처리 후 삭제
                prop.GetArrayElementAtIndex(toDelete).objectReferenceValue = null;
                prop.DeleteArrayElementAtIndex(toDelete);
                _so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_line);

                if (objToRemove != null)
                    Undo.DestroyObjectImmediate(objToRemove);

                AssetDatabase.SaveAssets();
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
                // 버튼 우하단 기준 screen 좌표로 Picker 창 열기
                var screenPt = GUIUtility.GUIToScreenPoint(
                    new Vector2(_addBtnRect.xMax, _addBtnRect.yMax));
                StoryModulePickerWindow.Show(AddModuleToLine, screenPt);
            }
        }

        // ── 모듈 추가 — sub-asset 생성 ────────────────

        private void AddModuleToLine(Type type)
        {
            if (_line == null) return;

            string assetPath = AssetDatabase.GetAssetPath(_line);
            if (string.IsNullOrEmpty(assetPath)) return;

            var module = ScriptableObject.CreateInstance(type) as StoryModuleSO;
            if (module == null) return;

            var attr = StoryModuleMetadataAttribute.Get(type);
            module.name = attr?.DisplayName ?? type.Name;

            AssetDatabase.AddObjectToAsset(module, _line);
            Undo.RegisterCreatedObjectUndo(module, $"Add Module: {module.name}");

            _so = new SerializedObject(_line);
            _so.Update();
            Undo.RecordObject(_line, $"Add Module: {module.name}");
            var prop = _so.FindProperty("modules");
            prop.arraySize++;
            prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = module;
            _so.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(_line);

            _expandedState[prop.arraySize - 1] = true;
            Refresh();
            Changed?.Invoke();
        }

        // ── Connectable 포트 스트립 (SO modules only) ──

        private void BuildConnectablePortStrip()
        {
            if (_line == null) return;

            bool anyConnectable = false;
            foreach (var soModule in _line.Modules)
            {
                if (soModule is IStoryGraphConnectableModule)
                {
                    anyConnectable = true;
                    break;
                }
            }
            if (!anyConnectable) return;

            var strip = new VisualElement
            {
                style =
                {
                    borderTopWidth = 1,
                    borderTopColor = new StyleColor(new Color(1f, 0.78f, 0.3f, 0.35f)),
                    paddingTop = 3,
                    paddingBottom = 3,
                    backgroundColor = new StyleColor(new Color(0.17f, 0.17f, 0.19f))
                }
            };

            int totalPorts = CountTotalConnectablePorts();
            var headerLbl = new Label($"▸ Connectable Ports  ({totalPorts} ports)")
            {
                style =
                {
                    fontSize = 9,
                    paddingLeft = 6,
                    paddingBottom = 1,
                    color = new StyleColor(new Color(1f, 0.78f, 0.3f, 0.75f))
                }
            };
            strip.Add(headerLbl);

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
                        label         = portLabel,
                        getConnection = () => (capturedSO as IStoryGraphConnectableModule)?.GetPortConnection(capturedPort),
                        setConnection = targetLineId =>
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
                        disconnect = () =>
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
                    slot.element = BuildPortRow(strip, slot, slotIdx);
                    _portSlots.Add(slot);
                }
            }

            Add(strip);
        }

        private VisualElement BuildPortRow(VisualElement portParent, PortSlot slot, int slotIdx)
        {
            string connection = slot.getConnection?.Invoke();
            bool   connected  = !string.IsNullOrWhiteSpace(connection);

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 22,
                    paddingLeft = 6,
                    paddingRight = 6
                }
            };

            string labelText = string.IsNullOrWhiteSpace(slot.label)
                ? $"Port {slotIdx}" : Trunc(slot.label, 22);
            var lbl = new Label(labelText)
            {
                style =
                {
                    flexGrow = 1,
                    fontSize = 10,
                    overflow = Overflow.Hidden,
                    color = new StyleColor(connected
                        ? new Color(0.45f, 0.95f, 0.5f)
                        : new Color(0.72f, 0.72f, 0.72f))
                }
            };
            row.Add(lbl);

            if (connected)
            {
                var idLbl = new Label($"→ {Trunc(connection, 14)}")
                {
                    style =
                    {
                        fontSize = 9,
                        color = new StyleColor(new Color(0.45f, 0.9f, 0.5f, 0.75f)),
                        marginRight = 4
                    }
                };
                row.Add(idLbl);
            }

            var port = new VisualElement
            {
                style =
                {
                    width = PortR * 2,
                    height = PortR * 2
                }
            };
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
                bool has = !string.IsNullOrWhiteSpace(_portSlots[capturedSlot].getConnection?.Invoke());
                evt.menu.AppendAction("Disconnect",
                    _ => ApplyDisconnect(capturedSlot),
                    has ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }));

            row.Add(port);
            portParent.Add(row);
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
            if (_line is null) return 0;
            int count = 0;
            foreach (var m in _line.Modules)
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
