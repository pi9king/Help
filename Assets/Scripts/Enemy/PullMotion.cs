using System;

namespace Help.Enemy
{
    // 끌어당김의 운동학(순수 static — UnityEngine 비의존).
    // 속박(EnemyStatus/EnemyAI)이 "결정 봉인"이라면 이쪽은 "외력" — 적의 의지와 무관하게 좌표를 향해 등속으로 끌린다.
    // EnemyBase가 AI 결정보다 우선하는 오버라이드로 적용한다.
    public static class PullMotion
    {
        // 앵커(보통 플레이어)를 향한 수평 속도. 정지 거리 안이면 0(경계에서의 좌우 진동 방지).
        public static float VelocityX(float selfX, float anchorX, float pullSpeed, float stopDistance)
        {
            float dx = anchorX - selfX;
            if (Math.Abs(dx) <= stopDistance) return 0f;
            return dx > 0f ? pullSpeed : -pullSpeed;
        }

        public static bool IsComplete(float selfX, float anchorX, float stopDistance)
            => Math.Abs(anchorX - selfX) <= stopDistance;
    }
}
