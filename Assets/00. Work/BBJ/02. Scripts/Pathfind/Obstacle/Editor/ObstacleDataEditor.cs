using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using PathFind.Obstacle;

namespace PathFind.Editor
{
    [CustomEditor(typeof(ObstacleData))]
    public class ObstacleDataEditor : UnityEditor.Editor
    {
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // Å¸ÀÏ ·»´õ »ó¼ö (Å©±â´Â °íÁ¤)
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        private const float TileW = 48f;
        private const float TileH = 24f;
        private const float CanvasPadY = 20f;
        private const float CanvasPadX = 40f;

        // ºä ¹üÀ§ ÇÑ°è
        private const int RangeMin = 1;
        private const int RangeMax = 8;

        private static readonly Color ColEmpty = new Color(0.28f, 0.56f, 0.87f, 0.18f);
        private static readonly Color ColBorder = new Color(0.28f, 0.56f, 0.87f, 0.50f);
        private static readonly Color ColBlocked = new Color(0.89f, 0.29f, 0.29f, 0.85f);
        private static readonly Color ColOrigin = new Color(0.98f, 0.78f, 0.46f, 0.95f);
        private static readonly Color ColHover = new Color(1.00f, 1.00f, 1.00f, 0.18f);
        private static readonly Color ColText = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color ColOriginText = new Color(0.38f, 0.20f, 0.02f, 1f);

        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // ¿¡µðÅÍ Àü¿ë »óÅÂ (SO¿¡ ÀúÀåÇÏÁö ¾ÊÀ½)
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        private int _viewMinX = -3;
        private int _viewMaxX = 3;
        private int _viewMinY = -3;
        private int _viewMaxY = 3;

        private HashSet<Vector2Int> _blocked = new HashSet<Vector2Int>();
        private Vector2Int _hovered = new Vector2Int(int.MinValue, int.MinValue);

        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // ±×¸®µå ºä Å©±â
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        private int ViewCols => _viewMaxX - _viewMinX + 1;
        private int ViewRows => _viewMaxY - _viewMinY + 1;

        private void OnEnable() => LoadFromSO();

        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // SO ¡ê HashSet µ¿±âÈ­
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        private void LoadFromSO()
        {
            var so = (ObstacleData)target;
            _blocked.Clear();
            if (so.blockedOffsets != null)
                foreach (var v in so.blockedOffsets)
                    _blocked.Add(v);
        }

        private void SaveToSO()
        {
            var so = (ObstacleData)target;
            Undo.RecordObject(so, "Edit Obstacle Offsets");
            so.blockedOffsets = _blocked.ToArray();
            EditorUtility.SetDirty(so);
        }

        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // ÁÂÇ¥ º¯È¯
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        private Vector2 GridToScreen(int gx, int gy, Rect canvas)
        {
            // ºä ¹üÀ§ÀÇ ¾ÆÀÌ¼Ò¸ÞÆ®¸¯ Áß½ÉÀÌ Äµ¹ö½º Áß¾Ó¿¡ ¿Àµµ·Ï
            float midGx = (_viewMinX + _viewMaxX) * 0.5f;
            float midGy = (_viewMinY + _viewMaxY) * 0.5f;

            float pivotX = canvas.x + canvas.width * 0.5f
                           - (midGx - midGy) * (TileW * 0.5f);
            float pivotY = canvas.y + CanvasPadY
                           + (ViewCols + ViewRows - 2) * 0.5f * (TileH * 0.5f);

            return new Vector2(
                pivotX + (gx - gy) * (TileW * 0.5f),
                pivotY + (gx + gy - _viewMinX - _viewMinY) * (TileH * 0.5f)
            );
        }

        private bool ScreenToGrid(Vector2 mouse, Rect canvas, out Vector2Int cell)
        {
            float bestDist = float.MaxValue;
            cell = default;
            bool found = false;

            for (int gy = _viewMinY; gy <= _viewMaxY; gy++)
                for (int gx = _viewMinX; gx <= _viewMaxX; gx++)
                {
                    Vector2 center = GridToScreen(gx, gy, canvas);
                    float dx = Mathf.Abs(mouse.x - center.x) / (TileW * 0.5f);
                    float dy = Mathf.Abs(mouse.y - center.y) / (TileH * 0.5f);
                    if (dx + dy <= 1.0f)
                    {
                        float dist = (mouse - center).sqrMagnitude;
                        if (dist < bestDist) { bestDist = dist; cell = new Vector2Int(gx, gy); found = true; }
                    }
                }
            return found;
        }

        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // ¸¶¸§¸ð ±×¸®±â
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        private static void DrawDiamond(Vector2 c, Color fill, Color border)
        {
            float hw = TileW * 0.5f, hh = TileH * 0.5f;
            Vector3[] v =
            {
                new Vector3(c.x,      c.y - hh, 0),
                new Vector3(c.x + hw, c.y,      0),
                new Vector3(c.x,      c.y + hh, 0),
                new Vector3(c.x - hw, c.y,      0),
            };
            Handles.DrawSolidRectangleWithOutline(v, fill, border);
        }

        private static void DrawLabel(Vector2 c, string text, Color color, int size = 9)
        {
            var s = new GUIStyle(EditorStyles.label)
            {
                fontSize = size,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }
            };
            GUI.Label(new Rect(c.x - TileW * 0.5f, c.y - TileH * 0.5f, TileW, TileH), text, s);
        }

        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // ¹üÀ§ ÄÁÆ®·Ñ ÇÑ Çà
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        private void DrawRangeRow(string axisLabel, ref int refMin, ref int refMax)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(axisLabel, EditorStyles.miniBoldLabel, GUILayout.Width(14));

                // À½¼ö ¹æÇâ
                GUILayout.Label("À½:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17))
                    && refMin > -RangeMax) { refMin--; Repaint(); }
                GUILayout.Label(refMin.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17))
                    && refMin < -RangeMin) { refMin++; Repaint(); }

                GUILayout.Space(10);

                // ¾ç¼ö ¹æÇâ
                GUILayout.Label("¾ç:", EditorStyles.miniLabel, GUILayout.Width(24));
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(17))
                    && refMax > RangeMin) { refMax--; Repaint(); }
                GUILayout.Label(refMax.ToString(), EditorStyles.miniLabel, GUILayout.Width(20));
                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(17))
                    && refMax < RangeMax) { refMax++; Repaint(); }

                GUILayout.FlexibleSpace();
                GUILayout.Label($"¹üÀ§ {refMin} ~ {refMax}", EditorStyles.miniLabel, GUILayout.Width(72));
            }
        }

        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // ºä ¹Û Â÷´Ü Å¸ÀÏ ¸ñ·Ï
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        private List<Vector2Int> OutOfViewBlocked() =>
            _blocked.Where(v =>
                v.x < _viewMinX || v.x > _viewMaxX ||
                v.y < _viewMinY || v.y > _viewMaxY).ToList();

        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        // OnInspectorGUI
        // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
        public override void OnInspectorGUI()
        {
            // ±âº» ÇÊµå
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Prefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isWalkable"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Blocked Offsets (ÄõÅÍºä ±×¸®µå)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "³ë¶õ Å¸ÀÏ = ¿øÁ¡(0,0)  ¡¤  »¡°£ Å¸ÀÏ = Â÷´Ü\n" +
                "Å¬¸¯ ¡æ Ãß°¡ / ´Ù½Ã Å¬¸¯ ¡æ Á¦°Å  ¡¤  ºä ¹üÀ§´Â ¿¡µðÅÍ¿¡¸¸ Àû¿ëµË´Ï´Ù.",
                MessageType.Info);

            // ¦¡¦¡ ºä ¹üÀ§ ÄÁÆ®·Ñ ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("±×¸®µå ºä ¹üÀ§  (¿¡µðÅÍ Àü¿ë ¡¤ SO ÀúÀå ¾È µÊ)", EditorStyles.miniBoldLabel);
            DrawRangeRow("X", ref _viewMinX, ref _viewMaxX);
            DrawRangeRow("Y", ref _viewMinY, ref _viewMaxY);

            // ¹üÀ§ ¹Û Â÷´Ü Å¸ÀÏ °æ°í
            var outOfView = OutOfViewBlocked();
            if (outOfView.Count > 0)
            {
                string coords = string.Join(", ", outOfView.Select(v => $"({v.x},{v.y})"));
                EditorGUILayout.HelpBox(
                    $"ºä ¹üÀ§ ¹Û ¼û°ÜÁø Â÷´Ü Å¸ÀÏ {outOfView.Count}°³: {coords}\n" +
                    "µ¥ÀÌÅÍ´Â SO¿¡ À¯ÁöµË´Ï´Ù. ¹üÀ§¸¦ ´Ã¸®¸é ´Ù½Ã Ç¥½ÃµË´Ï´Ù.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(6);

            // ¦¡¦¡ Äµ¹ö½º Å©±â °è»ê ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            float canvasW = TileW * (ViewCols + ViewRows) * 0.5f + CanvasPadX * 2f;
            float canvasH = TileH * (ViewCols + ViewRows) * 0.5f + CanvasPadY * 2f;
            Rect canvasRect = GUILayoutUtility.GetRect(canvasW, canvasH);
            EditorGUI.DrawRect(canvasRect, new Color(0.15f, 0.15f, 0.18f, 1f));

            // ¦¡¦¡ ÀÌº¥Æ® Ã³¸® ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            Event e = Event.current;
            if (canvasRect.Contains(e.mousePosition))
            {
                bool hit = ScreenToGrid(e.mousePosition, canvasRect, out Vector2Int hv);
                _hovered = hit ? hv : new Vector2Int(int.MinValue, int.MinValue);

                if (e.type == EventType.MouseDown && e.button == 0 && hit)
                {
                    if (hv != Vector2Int.zero)
                    {
                        if (_blocked.Contains(hv)) _blocked.Remove(hv);
                        else _blocked.Add(hv);
                        SaveToSO();
                    }
                    e.Use();
                }
                if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
                    Repaint();
            }
            else
            {
                _hovered = new Vector2Int(int.MinValue, int.MinValue);
            }

            // ¦¡¦¡ Å¸ÀÏ ·»´õ ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            if (e.type == EventType.Repaint)
            {
                for (int gy = _viewMinY; gy <= _viewMaxY; gy++)
                    for (int gx = _viewMinX; gx <= _viewMaxX; gx++)
                    {
                        var coord = new Vector2Int(gx, gy);
                        bool isOrig = coord == Vector2Int.zero;
                        bool isBlk = _blocked.Contains(coord);
                        bool isHov = coord == _hovered;
                        Vector2 ctr = GridToScreen(gx, gy, canvasRect);

                        Color fill = isOrig ? ColOrigin
                                     : isBlk ? ColBlocked
                                     : ColEmpty;
                        Color border = isOrig ? new Color(0.93f, 0.62f, 0.09f, 1f)
                                     : isBlk ? new Color(0.75f, 0.10f, 0.10f, 1f)
                                     : ColBorder;

                        DrawDiamond(ctr, fill, border);

                        if (isHov && !isOrig)
                            DrawDiamond(ctr, ColHover, Color.clear);

                        string lbl = isOrig ? "0,0" : $"{gx},{gy}";
                        Color tc = isOrig ? ColOriginText : ColText;
                        DrawLabel(ctr, lbl, tc);
                    }
            }

            // ¦¡¦¡ blockedOffsets ¸ñ·Ï ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            EditorGUILayout.Space(8);
            if (_blocked.Count == 0)
            {
                EditorGUILayout.LabelField("blockedOffsets: (¾øÀ½)", EditorStyles.miniLabel);
            }
            else
            {
                var parts = _blocked.OrderBy(v => v.y).ThenBy(v => v.x).Select(v => $"({v.x},{v.y})");
                EditorGUILayout.LabelField(
                    "blockedOffsets: " + string.Join("  ", parts),
                    EditorStyles.miniLabel);
            }

            // ¦¡¦¡ ¹öÆ° ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("ÀüÃ¼ ÃÊ±âÈ­", GUILayout.Width(90)))
                {
                    if (EditorUtility.DisplayDialog("ÃÊ±âÈ­ È®ÀÎ",
                        "¸ðµç Â÷´Ü Å¸ÀÏÀ» »èÁ¦ÇÏ½Ã°Ú½À´Ï±î?", "»èÁ¦", "Ãë¼Ò"))
                    {
                        _blocked.Clear();
                        SaveToSO();
                    }
                }
                if (GUILayout.Button("ºä ¹üÀ§ ¸®¼Â", GUILayout.Width(90)))
                {
                    _viewMinX = _viewMinY = -3;
                    _viewMaxX = _viewMaxY = 3;
                    Repaint();
                }
                GUILayout.FlexibleSpace();
            }
        }
    }
}