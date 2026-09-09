using System;
using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    public sealed class RoomTemplateParseResult
    {
        public List<string> Errors { get; } = new();
        public RoomTemplate Template { get; internal set; }
        public bool Success => Errors.Count == 0 && Template != null;
    }

    // ASCII 방 템플릿 → RoomTemplate. 순수 로직(파일 IO 없음)이라 EditMode에서 그대로 검증된다.
    //
    // 실패를 예외가 아니라 오류 목록으로 돌려주는 이유: 에디터 뷰어가 잘못된 템플릿을
    // "왜 잘못됐는지"와 함께 보여줘야 하기 때문. 한 장이 깨졌다고 나머지를 못 보면 안 된다.
    public static class RoomTemplateParser
    {
        public const char Wall = '#';
        public const char Ground = '=';
        public const char Platform = '-';
        public const char Air = '.';
        public const char Spike = '^';   // 피해 바닥(표준)
        public const char Pit = '~';     // 레거시 별칭 — Phase 3 템플릿 재작업 후 제거
        public const char Door = 'D';
        public const char ChancePlatform = '?';
        public const char ChanceEnemy = '%';
        public const char CommentPrefix = ';';

        // 지형이 아니라 "여기에 스폰" 지시. 칸 자체는 빈 공간이 된다.
        private const string MarkerChars = "peExlcb";

        public static RoomTemplateParseResult Parse(string text, string name) =>
            Parse(text?.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n') ?? Array.Empty<string>(), name);

        public static RoomTemplateParseResult Parse(IReadOnlyList<string> lines, string name)
        {
            var result = new RoomTemplateParseResult();
            var rows = StripCommentsAndBlanks(lines);

            if (rows.Count == 0)
            {
                result.Errors.Add($"[{name}] 내용이 없습니다.");
                return result;
            }

            int width = rows[0].Length;
            for (int r = 0; r < rows.Count; r++)
                if (rows[r].Length != width)
                    result.Errors.Add($"[{name}] {r + 1}번째 줄의 길이가 {rows[r].Length}로 첫 줄({width})과 다릅니다.");
            if (result.Errors.Count > 0) return result;

            int height = rows.Count;
            if (!TryResolveSizeClass(width, height, out var sizeClass))
            {
                result.Errors.Add($"[{name}] {width}x{height}는 어떤 방 크기 등급과도 맞지 않습니다. " +
                                  "RoomDimensions에 정의된 규격만 쓸 수 있습니다.");
                return result;
            }

            var tiles = new TileKind[width, height];
            var markers = new List<RoomMarker>();
            var doors = new Dictionary<Direction, Vector2Int>();
            var chancePlatforms = new List<Vector2Int>();
            var chanceEnemies = new List<Vector2Int>();

            for (int row = 0; row < height; row++)
            {
                int y = (height - 1) - row;   // 텍스트 첫 줄이 방 천장
                for (int x = 0; x < width; x++)
                {
                    char c = rows[row][x];
                    var cell = new Vector2Int(x, y);

                    switch (c)
                    {
                        case Wall: tiles[x, y] = TileKind.Wall; break;
                        case Ground: tiles[x, y] = TileKind.Floor; break;
                        case Platform: tiles[x, y] = TileKind.Platform; break;
                        case Air: tiles[x, y] = TileKind.Empty; break;
                        // D-12: 가시와 구덩이를 하나로 합쳤다. '^'가 표준이고
                        // '~'는 기존 템플릿이 남아 있는 동안만 받아주는 별칭이다(Phase 3에서 제거).
                        case Spike:
                        case Pit: tiles[x, y] = TileKind.Hazard; break;

                        case Door:
                            // 문은 셸에 구멍을 내지 않는다 — 벽 위에 문 타일을 덧그리고 E로 이동한다.
                            tiles[x, y] = TileKind.Wall;
                            RegisterDoor(result, name, doors, cell, width, height);
                            break;

                        case ChancePlatform:
                            tiles[x, y] = TileKind.Empty;
                            chancePlatforms.Add(cell);
                            break;

                        case ChanceEnemy:
                            tiles[x, y] = TileKind.Empty;
                            chanceEnemies.Add(cell);
                            break;

                        default:
                            if (MarkerChars.IndexOf(c) >= 0)
                            {
                                tiles[x, y] = TileKind.Empty;
                                markers.Add(new RoomMarker(c, cell));
                            }
                            else
                            {
                                result.Errors.Add($"[{name}] {row + 1}번째 줄 {x + 1}번째 칸에 알 수 없는 문자 '{c}'.");
                            }
                            break;
                    }
                }
            }

            // 문 조합마다 템플릿을 따로 만들지 않기 위해, 모든 템플릿은 4방향 문을 예약한다.
            // 실제로 연결이 없는 방향은 렌더 시 벽으로 남는다.
            foreach (Direction d in Enum.GetValues(typeof(Direction)))
                if (!doors.ContainsKey(d))
                    result.Errors.Add($"[{name}] {d} 문이 없습니다. 템플릿은 4방향 문 위치를 모두 예약해야 합니다.");

            if (result.Errors.Count > 0) return result;

            result.Template = new RoomTemplate(name, width, height, sizeClass, tiles,
                                               markers, doors, chancePlatforms, chanceEnemies);
            return result;
        }

        private static void RegisterDoor(RoomTemplateParseResult result, string name,
                                         Dictionary<Direction, Vector2Int> doors,
                                         Vector2Int cell, int width, int height)
        {
            Direction? dir = null;
            if (cell.y == height - 1) dir = Direction.North;
            else if (cell.y == 0) dir = Direction.South;
            else if (cell.x == 0) dir = Direction.West;
            else if (cell.x == width - 1) dir = Direction.East;

            if (!dir.HasValue)
            {
                result.Errors.Add($"[{name}] ({cell.x},{cell.y})의 문이 방 테두리 위에 있지 않습니다.");
                return;
            }
            if (doors.ContainsKey(dir.Value))
            {
                result.Errors.Add($"[{name}] {dir.Value} 문이 두 개 이상입니다.");
                return;
            }
            doors[dir.Value] = cell;
        }

        private static bool TryResolveSizeClass(int width, int height, out RoomSizeClass sizeClass)
        {
            foreach (RoomSizeClass c in Enum.GetValues(typeof(RoomSizeClass)))
            {
                var d = RoomDimensions.Of(c);
                if (d.Width == width && d.Height == height) { sizeClass = c; return true; }
            }
            sizeClass = RoomSizeClass.Small;
            return false;
        }

        // 주석(';')과 앞뒤 빈 줄을 걷어낸다. 격자 중간의 빈 줄은 길이 검사에서 잡힌다.
        private static List<string> StripCommentsAndBlanks(IReadOnlyList<string> lines)
        {
            var rows = new List<string>();
            foreach (var raw in lines)
            {
                if (raw == null) continue;
                var line = raw.TrimEnd();
                if (line.TrimStart().StartsWith(CommentPrefix.ToString(), StringComparison.Ordinal)) continue;
                rows.Add(line);
            }
            while (rows.Count > 0 && rows[0].Length == 0) rows.RemoveAt(0);
            while (rows.Count > 0 && rows[rows.Count - 1].Length == 0) rows.RemoveAt(rows.Count - 1);
            return rows;
        }
    }
}
