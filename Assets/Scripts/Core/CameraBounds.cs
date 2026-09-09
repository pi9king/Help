using UnityEngine;

namespace Help.Core
{
    // 카메라가 방 밖(빈 공간)을 비추지 않도록 위치를 방 경계 안으로 가둔다.
    // 방이 뷰보다 작으면 아예 방 중앙에 고정한다 — 작은 방에서 카메라가 흔들리면
    // 퍼즐 방의 "한눈에 보고 푼다"는 전제가 깨진다.
    //
    // MonoBehaviour 비의존 순수 로직.
    public static class CameraBounds
    {
        public static Vector2 Clamp(Vector2 target, Rect room, float halfWidth, float halfHeight)
        {
            return new Vector2(
                ClampAxis(target.x, room.xMin, room.xMax, halfWidth),
                ClampAxis(target.y, room.yMin, room.yMax, halfHeight));
        }

        private static float ClampAxis(float target, float min, float max, float half)
        {
            // 방이 뷰보다 좁으면 따라갈 여지가 없다 → 중앙 고정
            if (max - min <= half * 2f) return (min + max) * 0.5f;
            return Mathf.Clamp(target, min + half, max - half);
        }

        // 원점 중심으로 그려진 방의 월드 경계. 타일 중심이 정수 좌표이므로
        // 실제 범위는 양 끝 타일의 바깥면까지다.
        public static Rect RoomRect(int widthTiles, int heightTiles) =>
            new Rect(-widthTiles * 0.5f, -heightTiles * 0.5f, widthTiles, heightTiles);
    }
}
