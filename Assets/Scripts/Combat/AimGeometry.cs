using UnityEngine;

namespace Help.Combat
{
    public static class AimGeometry
    {
        public static Vector2 DirectionOrDefault(Vector2 direction) =>
            direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;

        public static Vector2 ForwardCenter(Vector2 origin, Vector2 direction, float reach) =>
            origin + DirectionOrDefault(direction) * reach;

        public static float AngleDegrees(Vector2 direction)
        {
            Vector2 normalized = DirectionOrDefault(direction);
            return Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;
        }

        // 조준각을 steps분할로 스냅한다. steps <= 1이면 연속각 그대로.
        // steps=4 -> 상하좌우, steps=8 -> 대각선 포함.
        public static Vector2 Snap(Vector2 direction, int steps)
        {
            Vector2 normalized = DirectionOrDefault(direction);
            if (steps <= 1) return normalized;

            float stepDegrees = 360f / steps;
            float snapped = Mathf.Round(AngleDegrees(normalized) / stepDegrees) * stepDegrees;
            float rad = snapped * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        }
    }
}
