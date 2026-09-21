using UnityEngine;

namespace Help.Player
{
    /// <summary>
    /// 쿼터뷰 이동의 순수 규칙. MonoBehaviour와 물리 적용은 PlayerController가 맡는다.
    /// </summary>
    public static class TopDownMovement
    {
        public static Vector2 Velocity(Vector2 input, float speed)
        {
            if (input.sqrMagnitude > 1f) input.Normalize();
            return input * Mathf.Max(0f, speed);
        }

        public static Vector2 DashDirection(Vector2 currentInput, Vector2 lastFacing)
        {
            Vector2 source = currentInput.sqrMagnitude > 0.0001f ? currentInput : lastFacing;
            if (source.sqrMagnitude <= 0.0001f) source = Vector2.down;
            return source.normalized;
        }
    }
}
