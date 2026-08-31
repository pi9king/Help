using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Help.Core;
using Help.Dungeon;

namespace Help.UI
{
    // 우상단 미니맵: 현재 층의 방 격자를 그린다. 현재 방·방문한 방·미방문 방·방 유형(보스/보물/상점)을 색과 글자로 구분.
    // RoomManager.OnRoomEntered로 방문/현재 위치를 갱신하고, 층 변경·사망 시 리셋. 화면은 런타임 자체 구성(UITheme).
    // HUD가 Canvas에 부착한다.
    public class MinimapUI : MonoBehaviour
    {
        private const float Cell = 18f;
        private const float Gap = 4f;
        private const float Step = Cell + Gap;
        private const float PanelW = 220f;
        private const float PanelH = 180f;

        private RoomManager _rm;
        private RectTransform _grid;
        private readonly HashSet<Vector2Int> _visited = new();

        private void Start()
        {
            BuildPanel();

            _rm = FindFirstObjectByType<RoomManager>();
            if (_rm != null)
            {
                _rm.OnRoomEntered += HandleRoomEntered;
                if (_rm.CurrentRoom != null) _visited.Add(Key(_rm.CurrentRoom));
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnFloorChanged += HandleReset;
                gm.OnRunReset += HandleReset;
            }

            Rebuild();
        }

        private void OnDestroy()
        {
            if (_rm != null) _rm.OnRoomEntered -= HandleRoomEntered;
            var gm = GameManager.Instance;
            if (gm != null) { gm.OnFloorChanged -= HandleReset; gm.OnRunReset -= HandleReset; }
        }

        private static Vector2Int Key(Room r) => new Vector2Int(r.X, r.Y);

        private void HandleRoomEntered(Room room)
        {
            _visited.Add(Key(room));
            Rebuild();
        }

        private void HandleReset()
        {
            _visited.Clear();
            if (_rm != null && _rm.CurrentRoom != null) _visited.Add(Key(_rm.CurrentRoom));
            Rebuild();
        }

        // ---------- 그리기 ----------

        private void Rebuild()
        {
            if (_grid == null) return;
            for (int i = _grid.childCount - 1; i >= 0; i--)
            {
                var c = _grid.GetChild(i);
                c.SetParent(null, false);
                Destroy(c.gameObject);
            }

            var map = GameManager.Instance?.CurrentMap;
            if (map == null) return;

            // 맵 좌표 범위 → 중심 계산(맵 중심을 패널 중심에 맞춤)
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var kv in map.Rooms)
            {
                var p = kv.Key;
                if (p.Item1 < minX) minX = p.Item1; if (p.Item1 > maxX) maxX = p.Item1;
                if (p.Item2 < minY) minY = p.Item2; if (p.Item2 > maxY) maxY = p.Item2;
            }
            float cx = (minX + maxX) * 0.5f, cy = (minY + maxY) * 0.5f;

            var current = _rm != null ? _rm.CurrentRoom : null;
            foreach (var kv in map.Rooms)
            {
                var room = kv.Value;
                bool visited = _visited.Contains(Key(room));
                bool adjacent = !visited && IsAdjacentToVisited(room);
                if (!visited && !adjacent) continue; // 방문·인접이 아니면 아예 표시하지 않음

                bool isCurrent = current != null && current.X == room.X && current.Y == room.Y;
                CreateCell(room, isCurrent, visited, adjacent, (room.X - cx) * Step, (room.Y - cy) * Step);
            }
        }

        // 방문한 방과 문으로 연결된(인접한) 방인가 → 아직 안 갔어도 "존재"를 미리 보여줄 대상.
        private bool IsAdjacentToVisited(Room room)
        {
            foreach (var n in new[] { room.North, room.South, room.East, room.West })
                if (n.HasValue && _visited.Contains(new Vector2Int(n.Value.x, n.Value.y))) return true;
            return false;
        }

        private void CreateCell(Room room, bool isCurrent, bool visited, bool adjacent, float lx, float ly)
        {
            var go = new GameObject($"Cell_{room.X}_{room.Y}", typeof(RectTransform));
            go.transform.SetParent(_grid, false);
            var img = go.AddComponent<Image>();
            img.color = CellColor(room, isCurrent, visited, adjacent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(isCurrent ? Cell + 2f : Cell, isCurrent ? Cell + 2f : Cell);
            rt.anchoredPosition = new Vector2(lx, ly);

            // 특수방(보스/보물/상점) 힌트 글자: 방문했거나 인접(입장 전 확인)일 때만. 일반방은 글자 없음.
            string glyph = TypeGlyph(room.Type);
            if (glyph != null && (visited || adjacent))
            {
                var tGo = new GameObject("G", typeof(RectTransform));
                tGo.transform.SetParent(go.transform, false);
                var t = tGo.AddComponent<Text>();
                t.font = UITheme.Font; t.fontSize = 12; t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter; t.text = glyph;
                t.color = room.Type == RoomType.Boss ? Color.white : UITheme.Panel;
                var trt = tGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = trt.offsetMax = Vector2.zero;
            }
        }

        private Color CellColor(Room room, bool isCurrent, bool visited, bool adjacent)
        {
            if (isCurrent) return UITheme.Accent; // 현재 방 = 노랑

            if (visited)
            {
                switch (room.Type)
                {
                    case RoomType.Boss: return UITheme.Danger;                 // 보스 = 빨강
                    case RoomType.Treasure:
                    case RoomType.Shop: return new Color(0.72f, 0.55f, 0.15f); // 보물/상점 = 골드
                    case RoomType.Tutorial: return UITheme.Accent2;            // 시작 = 시안
                    default: return new Color(0.55f, 0.62f, 0.72f);           // 방문한 일반 방 = 밝은 회청
                }
            }

            // 인접(미방문): 특수방은 흐린 유형 색으로 힌트, 일반방은 존재만(어둡게)
            switch (room.Type)
            {
                case RoomType.Boss: return new Color(0.5f, 0.15f, 0.15f);   // 흐린 빨강
                case RoomType.Treasure:
                case RoomType.Shop: return new Color(0.42f, 0.33f, 0.1f);   // 흐린 골드
                default: return UITheme.Slot;                               // 일반 미방문 = 어둡게
            }
        }

        private static string TypeGlyph(RoomType t)
        {
            switch (t)
            {
                case RoomType.Boss: return "B";
                case RoomType.Shop: return "$";
                case RoomType.Treasure: return "T";
                default: return null;
            }
        }

        // ---------- 패널 구성 ----------

        private void BuildPanel()
        {
            var panel = new GameObject("MinimapPanel", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            panel.AddComponent<Image>().color = UITheme.Border; // 테두리
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 1f); // 우상단
            prt.anchoredPosition = new Vector2(-16f, -16f);
            prt.sizeDelta = new Vector2(PanelW, PanelH);

            var inner = new GameObject("Inner", typeof(RectTransform));
            inner.transform.SetParent(panel.transform, false);
            var innerImg = inner.AddComponent<Image>();
            innerImg.color = new Color(UITheme.Panel.r, UITheme.Panel.g, UITheme.Panel.b, 0.85f);
            var irt = inner.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(3f, 3f); irt.offsetMax = new Vector2(-3f, -3f);

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(inner.transform, false);
            _grid = gridGo.GetComponent<RectTransform>();
            _grid.anchorMin = _grid.anchorMax = _grid.pivot = new Vector2(0.5f, 0.5f);
            _grid.anchoredPosition = Vector2.zero;
            _grid.sizeDelta = new Vector2(PanelW, PanelH);
        }
    }
}
