using Help.Enemy;

namespace Help.Combat
{
    public static class DamageCalculator
    {
        private const float ElementMismatchMultiplier = 0.1f;

        // 속성 잠금 없는 적 or 속성 일치 → 정상 데미지
        // 속성 불일치 → 대폭 감소
        public static int Calculate(int baseDamage, ElementType attackerElement, EnemyStats target)
        {
            if (target.LockedElement == ElementType.None)
                return baseDamage;

            if (attackerElement == target.LockedElement)
                return baseDamage;

            // 완전 면역이 아닌 "대폭 감소"이므로 최소 1은 보장 (정수 절삭으로 0이 되는 것 방지)
            return System.Math.Max(1, (int)(baseDamage * ElementMismatchMultiplier));
        }
    }
}
