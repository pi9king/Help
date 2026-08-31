using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    public enum TileKind { Floor, Wall }

    // 순수 로직: 사이드뷰 플랫포머용 방 셸을 계산한다.
    // 테두리만 solid — 바닥 한 줄(y==0)은 Floor, 나머지 테두리(천장/좌우 벽)는 Wall.
    // 내부는 빈 공간(공기)이라 딕셔너리에 포함하지 않는다(플레이어가 바닥을 딛고 벽에 막힘).
    // 문은 시각 마커 + E 상호작용으로 처리하므로 셸에 구멍을 뚫지 않는다(RoomManager가 오버레이).
    // MonoBehaviour(RoomManager)가 이 결과를 Tilemap에 그리고 콜라이더를 붙인다.
    public static class RoomLayout
    {
        public static Dictionary<Vector2Int, TileKind> Build(int width, int height)
        {
            var cells = new Dictionary<Vector2Int, TileKind>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool border = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    if (!border) continue; // 내부는 빈 공간
                    cells[new Vector2Int(x, y)] = (y == 0) ? TileKind.Floor : TileKind.Wall;
                }
            }

            return cells;
        }
    }
}
