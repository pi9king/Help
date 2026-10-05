using UnityEngine;

namespace Help.Combat
{
    // 공격 한 번 동안 몸통이 조준 방향으로 움직이는 거리(순수 로직 — EditMode 테스트 가능).
    //
    //   Windup   : 0 -> -pull     (뒤로 당긴다)
    //   Active   : -pull -> +reach (앞으로 내지른다)
    //   Recovery : +reach -> 0     (복귀)
    //
    // 스프라이트를 늘리지 않고 "제자리에서 팔만 흔드는" 느낌을 없애기 위한 것이다.
    // E 본체를 회전시키거나 찌그러뜨리지 않고 위치만 옮기므로 명세서 §26과 충돌하지 않는다.
    public static class AttackLunge
    {
        public static float Offset(float elapsed, float windup, float active, float recovery,
                                   float pull, float reach)
        {
            float total = windup + active + recovery;
            if (elapsed <= 0f || elapsed >= total) return 0f;

            if (elapsed < windup)
                return Mathf.Lerp(0f, -pull, Ease(elapsed / windup));

            float afterWindup = elapsed - windup;
            if (afterWindup < active)
                return Mathf.Lerp(-pull, reach, active > 0f ? Ease(afterWindup / active) : 1f);

            float afterActive = afterWindup - active;
            return Mathf.Lerp(reach, 0f, recovery > 0f ? Ease(afterActive / recovery) : 1f);
        }

        // 조준 방향으로의 실제 이동량. 쿼터뷰에서는 위아래 이동이 발을 지면에서 미끄러뜨려
        // 다리가 늘어나 보이므로 세로 성분만 verticalScale로 줄인다.
        public static Vector2 Displacement(Vector2 aim, float offset, float verticalScale)
        {
            return new Vector2(aim.x * offset, aim.y * offset * verticalScale);
        }

        // smoothstep. 시작과 끝을 눌러 줘서 전진이 기계적으로 보이지 않게 한다.
        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
