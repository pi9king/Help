using UnityEngine;

namespace Help.Player
{
    public enum Facing { Down, Left, Right, Up }

    // 조준 벡터 -> 4방향 시트의 행. Animator 상태 이름은 "{Action}_{Facing}" 규약을 따른다.
    public static class ECharacterFacing
    {
        // Death와 Skill은 Down 공통 클립만 있다 (animation_manifest.json).
        private static readonly string[] DownOnlyActions = { "Death", "Skill" };

        // enum.ToString()은 할당이 생긴다. 매 프레임 호출되므로 미리 깔아 둔다.
        private static readonly string[] FacingNames = { "Down", "Left", "Right", "Up" };

        public static Facing From(Vector2 aim)
        {
            if (aim.sqrMagnitude <= 0.0001f) return Facing.Down;

            // 45도 동률에서는 세로를 고른다 — 정면/후면이 더 분명하게 읽힌다.
            if (Mathf.Abs(aim.y) >= Mathf.Abs(aim.x))
                return aim.y >= 0f ? Facing.Up : Facing.Down;

            return aim.x >= 0f ? Facing.Right : Facing.Left;
        }

        public static string StateName(string action, Facing facing)
        {
            foreach (string downOnly in DownOnlyActions)
                if (action == downOnly) return action + "_Down";

            return action + "_" + FacingNames[(int)facing];
        }
    }
}
