using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Help.Dungeon;
using Help.Player;

namespace Help.EditorTools
{
    // ASCII 방 템플릿을 눈으로 보고 검증하는 창.
    //
    // 텍스트로 저작하면 대량 생성·자동 검증·diff를 얻지만 "이 공간이 뛰기 좋은가"는 안 보인다.
    // 이 창이 그 구멍을 메운다 — 지형을 그림으로 보여주고, 도달성 검사 결과를 색으로 겹쳐 준다.
    //
    // 편집은 텍스트 에디터에서 한다(저장 → 이 창의 새로고침). 텍스트가 언제나 진실이다.
    public class RoomTemplateWindow : EditorWindow
    {
        private const string RoomsDir = "Assets/Rooms";

        private Vector2 _listScroll, _reportScroll;
        private string _selectedPath;
        private RoomTemplateParseResult _parsed;
        private TemplateValidation _validation;
        private ResolvedRoom _preview;

        private bool _showReachability = true;
        private bool _allowDash = true;
        private int _chanceSeed;
        private ChanceMode _chanceMode = ChanceMode.Seeded;
        private Vector2Int? _probe;   // 클릭한 칸에서 갈 수 있는 범위를 본다

        [MenuItem("Help/Level/Room Template Viewer")]
        public static void Open() => GetWindow<RoomTemplateWindow>("Room Templates");

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawFileList();
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawToolbar();
                    DrawCanvas();
                    DrawReport();
                }
            }
        }

        // --- 좌측: 파일 목록 -------------------------------------------------

        private void DrawFileList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(210)))
            {
                EditorGUILayout.LabelField("Assets/Rooms", EditorStyles.boldLabel);
                if (GUILayout.Button("새로고침")) Reload();

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                foreach (var path in TemplateFiles())
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    bool ok = QuickParse(path)?.Success ?? false;

                    var style = new GUIStyle(EditorStyles.miniButton)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        normal = { textColor = ok ? EditorStyles.label.normal.textColor : new Color(1f, 0.45f, 0.4f) },
                    };
                    if (path == _selectedPath) style.fontStyle = FontStyle.Bold;

                    if (GUILayout.Button(ok ? name : "⚠ " + name, style)) Select(path);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private static IEnumerable<string> TemplateFiles()
        {
            if (!Directory.Exists(RoomsDir)) return Enumerable.Empty<string>();
            return Directory.GetFiles(RoomsDir, "*.txt").OrderBy(p => p);
        }

        private readonly Dictionary<string, RoomTemplateParseResult> _quick = new();

        private RoomTemplateParseResult QuickParse(string path)
        {
            if (_quick.TryGetValue(path, out var cached)) return cached;
            var r = RoomTemplateParser.Parse(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
            _quick[path] = r;
            return r;
        }

        private void Reload()
        {
            _quick.Clear();
            if (_selectedPath != null) Select(_selectedPath);
        }

        private void Select(string path)
        {
            _selectedPath = path;
            _probe = null;
            _quick.Remove(path);
            _parsed = QuickParse(path);
            Revalidate();
        }

        private void Revalidate()
        {
            _preview = null;
            _validation = null;
            if (_parsed == null || !_parsed.Success) return;

            _preview = RoomLayout.Resolve(_parsed.Template, _chanceMode, _chanceSeed);
            _validation = RoomTemplateValidator.Validate(_preview, _parsed.Template.Name,
                                                         PlatformerMetrics.PlayerDefault);
        }

        // --- 상단: 옵션 ------------------------------------------------------

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _chanceMode = (ChanceMode)EditorGUILayout.EnumPopup(_chanceMode, EditorStyles.toolbarPopup, GUILayout.Width(90));
                using (new EditorGUI.DisabledScope(_chanceMode != ChanceMode.Seeded))
                    _chanceSeed = EditorGUILayout.IntSlider("시드", _chanceSeed, 0, 40);
                if (EditorGUI.EndChangeCheck()) Revalidate();

                _showReachability = GUILayout.Toggle(_showReachability, "도달성", EditorStyles.toolbarButton, GUILayout.Width(60));
                EditorGUI.BeginChangeCheck();
                _allowDash = GUILayout.Toggle(_allowDash, "대시 허용", EditorStyles.toolbarButton, GUILayout.Width(70));
                if (EditorGUI.EndChangeCheck()) Repaint();

                GUILayout.FlexibleSpace();
                if (_probe.HasValue && GUILayout.Button("탐침 해제", EditorStyles.toolbarButton)) { _probe = null; Repaint(); }
            }
        }

        // --- 중앙: 격자 ------------------------------------------------------

        private static readonly Color CWall = new Color(0.38f, 0.40f, 0.46f);
        private static readonly Color CFloor = new Color(0.42f, 0.31f, 0.19f);
        private static readonly Color CPlatform = new Color(0.60f, 0.43f, 0.24f);
        private static readonly Color CSpike = new Color(0.80f, 0.28f, 0.28f); // 피해 바닥(D-12로 통합)
        private static readonly Color CAir = new Color(0.12f, 0.14f, 0.18f);
        private static readonly Color CReach = new Color(0.25f, 0.95f, 0.45f, 0.40f);
        private static readonly Color CUnreach = new Color(1f, 0.30f, 0.30f, 0.35f);
        private static readonly Color CDoor = new Color(1f, 0.80f, 0.10f);

        private void DrawCanvas()
        {
            if (_parsed == null) { EditorGUILayout.HelpBox("왼쪽에서 템플릿을 고르세요.", MessageType.Info); return; }
            if (!_parsed.Success) { EditorGUILayout.HelpBox("파싱 실패 — 아래 오류를 보세요.", MessageType.Error); return; }

            var room = _preview;
            var area = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            float cell = Mathf.Floor(Mathf.Min(area.width / room.Width, area.height / room.Height));
            if (cell < 2) return;

            float ox = area.x + (area.width - cell * room.Width) * 0.5f;
            float oy = area.y + (area.height - cell * room.Height) * 0.5f;

            Rect CellRect(int x, int y) => new Rect(ox + x * cell, oy + (room.Height - 1 - y) * cell, cell, cell);

            for (int x = 0; x < room.Width; x++)
                for (int y = 0; y < room.Height; y++)
                    EditorGUI.DrawRect(CellRect(x, y), ColorOf(room.Tiles[x, y]));

            if (_showReachability) DrawReachabilityOverlay(room, CellRect);

            foreach (var kv in room.Doors)
            {
                var r = CellRect(kv.Value.x, kv.Value.y);
                EditorGUI.DrawRect(r, CDoor);
                if (cell >= 9) GUI.Label(r, kv.Key.ToString().Substring(0, 1), Mini(Color.black));
            }

            foreach (var marker in room.Markers)
            {
                var r = CellRect(marker.Cell.x, marker.Cell.y);
                if (cell >= 8) GUI.Label(r, marker.Symbol.ToString(), Mini(Color.white));
                else EditorGUI.DrawRect(r, Color.cyan);
            }

            HandleProbeClick(area, room, ox, oy, cell);
        }

        private void DrawReachabilityOverlay(ResolvedRoom room, System.Func<int, int, Rect> cellRect)
        {
            var metrics = PlatformerMetrics.PlayerDefault;
            var options = new AnalyzerOptions { AllowDash = _allowDash };

            // 탐침이 있으면 "여기서 갈 수 있는 곳", 없으면 "문에서 갈 수 있는 곳"
            IEnumerable<Vector2Int> starts = _probe.HasValue
                ? new[] { _probe.Value }
                : (_validation != null ? _validation.DoorAccess.Values : System.Array.Empty<Vector2Int>());
            if (!starts.Any()) return;

            var set = ReachabilityAnalyzer.Analyze(room.Tiles, starts, metrics, options);

            for (int x = 0; x < room.Width; x++)
                for (int y = 0; y < room.Height; y++)
                {
                    if (ResolvedRoom.BlocksMovement(room.Tiles[x, y])) continue;
                    bool standable = ResolvedRoom.IsStandable(room.TileAt(x, y - 1))
                                     && !ResolvedRoom.IsHazard(room.TileAt(x, y - 1));
                    if (!standable) continue;
                    EditorGUI.DrawRect(cellRect(x, y), set.Contains(new Vector2Int(x, y)) ? CReach : CUnreach);
                }
        }

        private void HandleProbeClick(Rect area, ResolvedRoom room, float ox, float oy, float cell)
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !area.Contains(e.mousePosition)) return;

            int x = Mathf.FloorToInt((e.mousePosition.x - ox) / cell);
            int y = (room.Height - 1) - Mathf.FloorToInt((e.mousePosition.y - oy) / cell);
            if (x < 0 || y < 0 || x >= room.Width || y >= room.Height) return;

            _probe = new Vector2Int(x, y);
            e.Use();
            Repaint();
        }

        private static Color ColorOf(TileKind k) => k switch
        {
            TileKind.Wall => CWall,
            TileKind.Floor => CFloor,
            TileKind.Platform => CPlatform,
            TileKind.Hazard => CSpike,
            _ => CAir,
        };

        private static GUIStyle Mini(Color c) => new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = c },
        };

        // --- 하단: 검증 결과 --------------------------------------------------

        private void DrawReport()
        {
            _reportScroll = EditorGUILayout.BeginScrollView(_reportScroll, GUILayout.Height(150));

            if (_parsed != null && !_parsed.Success)
                foreach (var err in _parsed.Errors) EditorGUILayout.HelpBox(err, MessageType.Error);

            if (_validation != null)
            {
                if (_validation.Ok)
                {
                    EditorGUILayout.HelpBox("모든 문과 마커에 도달할 수 있습니다.", MessageType.Info);
                }
                else
                {
                    foreach (var err in _validation.Errors) EditorGUILayout.HelpBox(err, MessageType.Warning);
                }

                if (_probe.HasValue)
                    EditorGUILayout.LabelField($"탐침 ({_probe.Value.x}, {_probe.Value.y}) 기준으로 표시 중 — 초록=갈 수 있음, 빨강=못 감");
            }

            if (_parsed != null && _parsed.Success)
            {
                var t = _parsed.Template;
                EditorGUILayout.LabelField(
                    $"{t.Width}x{t.Height} ({t.SizeClass})  ·  마커 {t.Markers.Count}  ·  확률칸 {t.ChancePlatforms.Count + t.ChanceEnemies.Count}");

                var m = PlatformerMetrics.PlayerDefault;
                EditorGUILayout.LabelField(
                    $"문법: 단차 ≤3타일  ·  갭 ≤5칸(대시 시 7칸)  ·  통로 높이 ≥{m.RequiredHeadroomTiles}칸  ·  점프 {m.MaxJumpHeight:F2}타일");
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
