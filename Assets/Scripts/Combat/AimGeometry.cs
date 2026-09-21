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
    }
}
