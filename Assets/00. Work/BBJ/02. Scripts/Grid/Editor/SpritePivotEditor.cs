using UnityEngine;
namespace BBJ.GridSystem.Editor
{

    using UnityEngine;
    using UnityEditor;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 쿼터뷰 게임용 스프라이트 피봇 에디터
    /// Editor > Tools > Sprite Pivot Editor 에서 열 수 있습니다.
    /// </summary>
    public class SpritePivotEditor : EditorWindow
    {
        // ── 탭 & 스크롤 ──────────────────────────────────────────────
        private int _tab = 0;
        private readonly string[] _tabs = { "자동 설정", "수동 편집", "배치 처리" };
        private Vector2 _scrollPos;

        // ── 선택된 스프라이트 ─────────────────────────────────────────
        private List<Texture2D> _selectedTextures = new();
        private Texture2D _previewTex;
        private Sprite _previewSprite;

        // ── 자동 설정 옵션 ────────────────────────────────────────────
        private enum AutoPivotMode
        {
            QuarterViewFoot,   // 쿼터뷰: 발판 중앙 하단
            BottomCenter,      // 하단 중앙
            Center,            // 정중앙
            TopCenter,         // 상단 중앙
            BottomLeft,        // 좌하단
            Custom             // 직접 지정
        }
        private AutoPivotMode _autoMode = AutoPivotMode.QuarterViewFoot;
        private Vector2 _customPivot = new(0.5f, 0.0f);

        // 쿼터뷰 전용 옵션
        private float _quarterFootRatio = 0.5f;  // 가로 중앙 비율 (0~1)
        private float _quarterFootY = 0.08f; // 발바닥 Y 오프셋 (0~1, 아래서 위)

        // ── 수동 편집 ─────────────────────────────────────────────────
        private Vector2 _manualPivot = new(0.5f, 0f);
        private bool _showGrid = true;
        private bool _snapToPixel = false;
        private float _previewZoom = 1f;
        private Vector2 _previewScroll;

        // ── 배치 처리 ────────────────────────────────────────────────
        private bool _batchUseAuto = true;
        private bool _batchConfirmed = false;

        // ── 스타일 캐시 ──────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _infoBoxStyle;
        private bool _stylesInit = false;

        // ── 색상 ─────────────────────────────────────────────────────
        private static readonly Color AccentBlue = new(0.22f, 0.55f, 0.95f);
        private static readonly Color AccentGreen = new(0.25f, 0.80f, 0.45f);
        private static readonly Color AccentAmber = new(0.95f, 0.72f, 0.20f);
        private static readonly Color PanelBg = new(0.18f, 0.18f, 0.20f);
        private static readonly Color PreviewBg = new(0.12f, 0.12f, 0.14f);

        // ─────────────────────────────────────────────────────────────
        [MenuItem("Tools/Sprite Pivot Editor")]
        public static void Open() =>
            GetWindow<SpritePivotEditor>("Sprite Pivot Editor").Show();

        // ─────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            RefreshSelection();
        }

        private void OnSelectionChange()
        {
            RefreshSelection();
            Repaint();
        }

        private void RefreshSelection()
        {
            _selectedTextures = Selection.objects
                .OfType<Texture2D>()
                .ToList();

            _previewTex = _selectedTextures.Count > 0 ? _selectedTextures[0] : null;
            _previewSprite = _previewTex != null ? GetFirstSprite(_previewTex) : null;
        }

        // ─────────────────────────────────────────────────────────────
        private void InitStyles()
        {
            if (_stylesInit) return;
            _stylesInit = true;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            _headerStyle.normal.textColor = Color.white;

            _subHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Italic
            };
            _subHeaderStyle.normal.textColor = new Color(0.7f, 0.7f, 0.75f);

            _infoBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                padding = new RectOffset(8, 8, 6, 6)
            };
        }

        // ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            InitStyles();

            // 상단 헤더 바
            DrawTopHeader();

            // 선택 상태 표시
            DrawSelectionStatus();

            // 탭 바
            _tab = GUILayout.Toolbar(_tab, _tabs, GUILayout.Height(28));
            EditorGUILayout.Space(4);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_tab)
            {
                case 0: DrawAutoTab(); break;
                case 1: DrawManualTab(); break;
                case 2: DrawBatchTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        // ─────────────────────────────────────────────────────────────
        private void DrawTopHeader()
        {
            var rect = EditorGUILayout.GetControlRect(false, 40);
            EditorGUI.DrawRect(rect, PanelBg);

            var iconRect = new Rect(rect.x + 10, rect.y + 8, 24, 24);
            var titleRect = new Rect(rect.x + 40, rect.y + 4, rect.width - 50, 20);
            var subRect = new Rect(rect.x + 40, rect.y + 22, rect.width - 50, 14);

            // 쿼터뷰 아이콘 (다이아몬드)
            DrawDiamondIcon(iconRect, AccentBlue);

            EditorGUI.LabelField(titleRect, "Sprite Pivot Editor", _headerStyle);
            EditorGUI.LabelField(subRect, "쿼터뷰 게임용 스프라이트 피봇 도구", _subHeaderStyle);
        }

        private void DrawDiamondIcon(Rect rect, Color color)
        {
            var cx = rect.x + rect.width * 0.5f;
            var cy = rect.y + rect.height * 0.5f;
            var hw = rect.width * 0.45f;
            var hh = rect.height * 0.45f;

            Handles.BeginGUI();
            Handles.color = color;
            var pts = new Vector3[]
            {
            new(cx,      cy - hh, 0),
            new(cx + hw, cy,      0),
            new(cx,      cy + hh, 0),
            new(cx - hw, cy,      0),
            };
            Handles.DrawAAConvexPolygon(pts);
            Handles.EndGUI();
        }

        // ─────────────────────────────────────────────────────────────
        private void DrawSelectionStatus()
        {
            EditorGUILayout.Space(4);
            int count = _selectedTextures.Count;

            if (count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Project 창에서 Texture2D 에셋을 선택하세요.\n" +
                    "여러 파일을 동시에 선택하면 배치 처리가 가능합니다.",
                    MessageType.Info);
            }
            else
            {
                var origColor = GUI.color;
                GUI.color = count == 1 ? AccentGreen : AccentAmber;
                EditorGUILayout.HelpBox(
                    count == 1
                        ? $"선택됨: {_previewTex.name}  ({_previewTex.width} x {_previewTex.height})"
                        : $"{count}개 텍스처 선택됨 — 배치 처리 탭을 사용하세요",
                    MessageType.None);
                GUI.color = origColor;
            }

            EditorGUILayout.Space(2);
        }

        // ═════════════════════════════════════════════════════════════
        //  TAB 0 : 자동 설정
        // ═════════════════════════════════════════════════════════════
        private void DrawAutoTab()
        {
            EditorGUILayout.LabelField("피봇 자동 설정 모드", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // 모드 선택
            _autoMode = (AutoPivotMode)EditorGUILayout.EnumPopup("피봇 프리셋", _autoMode);
            EditorGUILayout.Space(4);

            // 모드별 세부 옵션
            switch (_autoMode)
            {
                case AutoPivotMode.QuarterViewFoot:
                    DrawQuarterViewOptions();
                    break;
                case AutoPivotMode.Custom:
                    _customPivot = EditorGUILayout.Vector2Field("커스텀 피봇 (0~1)", _customPivot);
                    _customPivot = ClampPivot(_customPivot);
                    break;
                default:
                    var desc = _autoMode switch
                    {
                        AutoPivotMode.BottomCenter => "(0.5, 0.0) — 하단 중앙",
                        AutoPivotMode.Center => "(0.5, 0.5) — 정중앙",
                        AutoPivotMode.TopCenter => "(0.5, 1.0) — 상단 중앙",
                        AutoPivotMode.BottomLeft => "(0.0, 0.0) — 좌하단",
                        _ => ""
                    };
                    EditorGUILayout.HelpBox($"선택된 프리셋: {desc}", MessageType.None);
                    break;
            }

            EditorGUILayout.Space(8);
            DrawPivotPreview(GetAutoPivot());

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(_selectedTextures.Count == 0))
            {
                if (DrawAccentButton("선택된 스프라이트에 적용", AccentBlue))
                    ApplyPivots(_selectedTextures, GetAutoPivot());
            }
        }

        private void DrawQuarterViewOptions()
        {
            EditorGUILayout.HelpBox(
                "쿼터뷰(등각) 게임에 최적화된 피봇입니다.\n" +
                "캐릭터·오브젝트의 발판 중앙 하단에 피봇을 설정해 Z-소팅을 정확하게 합니다.",
                MessageType.None);

            EditorGUILayout.Space(4);

            _quarterFootRatio = EditorGUILayout.Slider("가로 중심 비율", _quarterFootRatio, 0f, 1f);
            _quarterFootY = EditorGUILayout.Slider("발바닥 Y 오프셋", _quarterFootY, 0f, 0.5f);

            EditorGUILayout.Space(2);
            var pv = GetAutoPivot();
            EditorGUILayout.LabelField($"계산된 피봇: ({pv.x:F3}, {pv.y:F3})",
                EditorStyles.miniLabel);
        }

        private Vector2 GetAutoPivot() => _autoMode switch
        {
            AutoPivotMode.QuarterViewFoot => new Vector2(_quarterFootRatio, _quarterFootY),
            AutoPivotMode.BottomCenter => new Vector2(0.5f, 0.0f),
            AutoPivotMode.Center => new Vector2(0.5f, 0.5f),
            AutoPivotMode.TopCenter => new Vector2(0.5f, 1.0f),
            AutoPivotMode.BottomLeft => new Vector2(0.0f, 0.0f),
            AutoPivotMode.Custom => _customPivot,
            _ => new Vector2(0.5f, 0.0f)
        };

        // ═════════════════════════════════════════════════════════════
        //  TAB 1 : 수동 편집
        // ═════════════════════════════════════════════════════════════
        private void DrawManualTab()
        {
            if (_previewTex == null)
            {
                EditorGUILayout.HelpBox("텍스처를 선택하면 여기서 피봇을 직접 편집할 수 있습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("수동 피봇 편집", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // 수치 입력
            _manualPivot = EditorGUILayout.Vector2Field("피봇 값 (0~1)", _manualPivot);
            _manualPivot = ClampPivot(_manualPivot);

            EditorGUILayout.Space(4);

            // 옵션
            _showGrid = EditorGUILayout.Toggle("그리드 표시", _showGrid);
            _snapToPixel = EditorGUILayout.Toggle("픽셀 스냅", _snapToPixel);
            _previewZoom = EditorGUILayout.Slider("미리보기 배율", _previewZoom, 0.5f, 4f);

            EditorGUILayout.Space(6);

            // 빠른 설정 버튼
            DrawQuickSetButtons();

            EditorGUILayout.Space(8);

            // 인터랙티브 미리보기
            DrawInteractivePivotPreview();

            EditorGUILayout.Space(8);

            // 픽셀 좌표 표시
            if (_previewTex != null)
            {
                int px = Mathf.RoundToInt(_manualPivot.x * _previewTex.width);
                int py = Mathf.RoundToInt(_manualPivot.y * _previewTex.height);
                EditorGUILayout.LabelField($"픽셀 좌표: ({px}, {py})  |  텍스처: {_previewTex.width}x{_previewTex.height}",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(6);

            using (new EditorGUI.DisabledScope(_selectedTextures.Count == 0))
            {
                if (DrawAccentButton("현재 텍스처에 적용", AccentBlue))
                    ApplyPivots(new List<Texture2D> { _previewTex }, _manualPivot);
            }
        }

        private void DrawQuickSetButtons()
        {
            EditorGUILayout.LabelField("빠른 설정", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var presets = new (string label, Vector2 pivot)[]
                {
                ("좌상", new(0f, 1f)), ("중상", new(0.5f, 1f)), ("우상", new(1f, 1f)),
                ("좌중", new(0f, 0.5f)), ("중앙", new(0.5f, 0.5f)), ("우중", new(1f, 0.5f)),
                ("좌하", new(0f, 0f)), ("중하", new(0.5f, 0f)), ("우하", new(1f, 0f)),
                };

                foreach (var (lbl, pv) in presets)
                {
                    if (GUILayout.Button(lbl, GUILayout.Width(38), GUILayout.Height(24)))
                        _manualPivot = pv;
                }
            }
        }

        // ─── 인터랙티브 미리보기 ─────────────────────────────────────
        private void DrawInteractivePivotPreview()
        {
            float displayW = Mathf.Min(position.width - 32, _previewTex.width * _previewZoom);
            float displayH = displayW * _previewTex.height / _previewTex.width;
            displayH = Mathf.Min(displayH, 300f);
            displayW = displayH * _previewTex.width / _previewTex.height;

            var previewRect = GUILayoutUtility.GetRect(displayW, displayH + 2);
            previewRect = new Rect(
                previewRect.x + (previewRect.width - displayW) * 0.5f,
                previewRect.y,
                displayW, displayH);

            // 배경
            EditorGUI.DrawRect(previewRect, PreviewBg);

            // 스프라이트
            if (Event.current.type == EventType.Repaint && _previewTex != null)
            {
                GUI.DrawTexture(previewRect, _previewTex, ScaleMode.StretchToFill, true);
            }

            // 그리드
            if (_showGrid && Event.current.type == EventType.Repaint)
                DrawPreviewGrid(previewRect);

            // 피봇 크로스헤어
            float crossX = previewRect.x + _manualPivot.x * previewRect.width;
            float crossY = previewRect.y + (1f - _manualPivot.y) * previewRect.height;

            if (Event.current.type == EventType.Repaint)
            {
                Handles.BeginGUI();
                Handles.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                Handles.DrawLine(new Vector3(crossX - 12, crossY), new Vector3(crossX + 12, crossY));
                Handles.DrawLine(new Vector3(crossX, crossY - 12), new Vector3(crossX, crossY + 12));
                Handles.color = Color.white;
                Handles.DrawWireDisc(new Vector3(crossX, crossY, 0), Vector3.forward, 5f);
                Handles.EndGUI();
            }

            // 마우스 드래그로 피봇 이동
            var ev = Event.current;
            if ((ev.type == EventType.MouseDown || ev.type == EventType.MouseDrag)
                && previewRect.Contains(ev.mousePosition))
            {
                float nx = (ev.mousePosition.x - previewRect.x) / previewRect.width;
                float ny = 1f - (ev.mousePosition.y - previewRect.y) / previewRect.height;

                if (_snapToPixel && _previewTex != null)
                {
                    nx = Mathf.Round(nx * _previewTex.width) / _previewTex.width;
                    ny = Mathf.Round(ny * _previewTex.height) / _previewTex.height;
                }

                _manualPivot = ClampPivot(new Vector2(nx, ny));
                ev.Use();
                Repaint();
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("미리보기 영역을 클릭·드래그하여 피봇을 이동할 수 있습니다.",
                EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawPreviewGrid(Rect r)
        {
            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.08f);

            for (int i = 1; i < 4; i++)
            {
                float x = r.x + r.width * i / 4f;
                float y = r.y + r.height * i / 4f;
                Handles.DrawLine(new Vector3(x, r.y), new Vector3(x, r.yMax));
                Handles.DrawLine(new Vector3(r.x, y), new Vector3(r.xMax, y));
            }

            Handles.EndGUI();
        }

        // ═════════════════════════════════════════════════════════════
        //  TAB 2 : 배치 처리
        // ═════════════════════════════════════════════════════════════
        private void DrawBatchTab()
        {
            EditorGUILayout.LabelField("배치 피봇 처리", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (_selectedTextures.Count == 0)
            {
                EditorGUILayout.HelpBox("Project 창에서 여러 텍스처를 선택하세요.", MessageType.Info);
                return;
            }

            // 선택 목록 표시
            EditorGUILayout.LabelField($"대상 파일 ({_selectedTextures.Count}개):", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var tex in _selectedTextures)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(EditorGUIUtility.ObjectContent(tex, typeof(Texture2D)),
                            GUILayout.Height(18), GUILayout.Width(220));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField($"{tex.width}x{tex.height}",
                            EditorStyles.miniLabel, GUILayout.Width(80));
                    }
                }
            }

            EditorGUILayout.Space(8);

            // 일괄 적용 방식 선택
            _batchUseAuto = EditorGUILayout.Toggle("자동 피봇 사용", _batchUseAuto);
            EditorGUILayout.Space(2);

            if (_batchUseAuto)
            {
                _autoMode = (AutoPivotMode)EditorGUILayout.EnumPopup("피봇 프리셋", _autoMode);
                if (_autoMode == AutoPivotMode.QuarterViewFoot)
                    DrawQuarterViewOptions();
                else if (_autoMode == AutoPivotMode.Custom)
                    _customPivot = EditorGUILayout.Vector2Field("커스텀 피봇", _customPivot);
            }
            else
            {
                _manualPivot = EditorGUILayout.Vector2Field("피봇 값 (0~1)", _manualPivot);
                _manualPivot = ClampPivot(_manualPivot);
            }

            EditorGUILayout.Space(8);

            var pivot = _batchUseAuto ? GetAutoPivot() : _manualPivot;
            DrawPivotPreview(pivot);

            EditorGUILayout.Space(6);

            // 확인 체크박스 + 실행
            _batchConfirmed = EditorGUILayout.Toggle(
                $"'{_selectedTextures.Count}개' 파일 수정을 확인했습니다",
                _batchConfirmed);

            using (new EditorGUI.DisabledScope(!_batchConfirmed))
            {
                if (DrawAccentButton($"전체 {_selectedTextures.Count}개 일괄 적용", AccentAmber))
                {
                    ApplyPivots(_selectedTextures, pivot);
                    _batchConfirmed = false;
                }
            }
        }

        // ═════════════════════════════════════════════════════════════
        //  공통 미리보기 위젯
        // ═════════════════════════════════════════════════════════════
        private void DrawPivotPreview(Vector2 pivot)
        {
            EditorGUILayout.LabelField("피봇 위치 미리보기", EditorStyles.boldLabel);

            const float SZ = 80f;
            var rect = GUILayoutUtility.GetRect(SZ, SZ);
            rect = new Rect(rect.x + (rect.width - SZ) * 0.5f, rect.y, SZ, SZ);

            EditorGUI.DrawRect(rect, PreviewBg);

            float px = rect.x + pivot.x * SZ;
            float py = rect.y + (1f - pivot.y) * SZ;

            Handles.BeginGUI();

            // 가이드라인
            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            Handles.DrawLine(new Vector3(rect.x, py), new Vector3(rect.xMax, py));
            Handles.DrawLine(new Vector3(px, rect.y), new Vector3(px, rect.yMax));

            // 피봇 마커
            Handles.color = AccentBlue;
            Handles.DrawLine(new Vector3(px - 8, py), new Vector3(px + 8, py));
            Handles.DrawLine(new Vector3(px, py - 8), new Vector3(px, py + 8));
            Handles.DrawWireDisc(new Vector3(px, py, 0), Vector3.forward, 4f);

            Handles.EndGUI();

            EditorGUILayout.LabelField(
                $"X: {pivot.x:F3}  Y: {pivot.y:F3}",
                EditorStyles.centeredGreyMiniLabel);
        }

        // ═════════════════════════════════════════════════════════════
        //  피봇 적용 (핵심 로직)
        // ═════════════════════════════════════════════════════════════
        private void ApplyPivots(List<Texture2D> textures, Vector2 pivot)
        {
            int success = 0, skip = 0;

            foreach (var tex in textures)
            {
                string path = AssetDatabase.GetAssetPath(tex);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { skip++; continue; }

                // Sprite 타입이 아니면 변환
                if (importer.textureType != TextureImporterType.Sprite)
                    importer.textureType = TextureImporterType.Sprite;

                // Sprite 모드가 Single이면 바로 설정
                // Multiple이면 각 서브스프라이트의 alignment를 Custom으로 변경
                if (importer.spriteImportMode == SpriteImportMode.Multiple)
                {
                    var sheetData =  importer.spritesheet;
                    for (int i = 0; i < sheetData.Length; i++)
                    {
                        sheetData[i].alignment = (int)SpriteAlignment.Custom;
                        sheetData[i].pivot = pivot;
                    }
                    importer.spritesheet = sheetData;
                }
                else
                {
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);          // 현재 설정 읽기
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = pivot;
                    importer.SetTextureSettings(settings);
                }

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                success++;
            }

            // 결과 메시지
            string msg = skip == 0
                ? $"완료: {success}개 스프라이트 피봇을 ({pivot.x:F3}, {pivot.y:F3})으로 설정했습니다."
                : $"완료: {success}개 성공, {skip}개 건너뜀 (Sprite 타입이 아닌 파일).";

            EditorUtility.DisplayDialog("Sprite Pivot Editor", msg, "확인");
            Repaint();
        }

        // ═════════════════════════════════════════════════════════════
        //  유틸리티
        // ═════════════════════════════════════════════════════════════
        private static Sprite GetFirstSprite(Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Vector2 ClampPivot(Vector2 v) =>
            new(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y));

        private bool DrawAccentButton(string label, Color color)
        {
            var origBg = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool result = GUILayout.Button(label, GUILayout.Height(34));
            GUI.backgroundColor = origBg;
            return result;
        }
    }
}