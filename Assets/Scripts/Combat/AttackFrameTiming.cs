namespace Help.Combat
{
    // 공격 한 번 동안 보여 줄 Attack 시트 프레임(순수 로직 — EditMode 테스트 가능).
    //
    // 시트 6프레임: 0 중립 · 1 예비동작 · 2 치켜듦 · 3 타격 · 4 팔로스루 · 5 회수
    //
    //   Windup   : 앞 40% 예비동작, 나머지 치켜듦
    //   Active   : 앞 60% 타격(판정 순간에 그림을 멈춰 무게를 준다), 나머지 팔로스루
    //   Recovery : 회수
    //
    // 클립을 균일 속도로 늘이고 줄이면 예비동작 그림이 판정 시작 뒤까지 남아 타격과 그림이
    // 어긋났다. 중립 프레임은 공격 직전 Idle과 같은 그림이라 건너뛴다.
    public static class AttackFrameTiming
    {
        public const int Neutral = 0;
        public const int Anticipation = 1;
        public const int WindUp = 2;
        public const int Impact = 3;
        public const int FollowThrough = 4;
        public const int Recovery = 5;

        public const int FrameCount = 6;

        private const float AnticipationShare = 0.4f;
        private const float ImpactShare = 0.6f;

        public static int FrameAt(float elapsed, float windup, float active, float recovery)
        {
            if (elapsed < windup)
                return elapsed < windup * AnticipationShare ? Anticipation : WindUp;

            float afterWindup = elapsed - windup;
            if (afterWindup < active)
                return afterWindup < active * ImpactShare ? Impact : FollowThrough;

            return Recovery;
        }

        // Animator 정규화 시간. 클립은 6프레임 뒤에 마지막 프레임 유지 키가 하나 더 있어 7칸 길이다.
        public static float ClipTime(int frame) => (frame + 0.5f) / (FrameCount + 1);
    }
}
