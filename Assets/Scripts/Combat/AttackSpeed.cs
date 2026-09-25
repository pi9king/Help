namespace Help.Combat
{
    // 무기 공격 속도 배수(ItemDefinition.AttackSpeedMult)를 시간에 적용하는 단일 규칙.
    // 공격 간격(PlayerController)과 모션 타이밍(PlayerAttack)이 둘 다 이걸 쓴다 —
    // 한쪽만 줄이면 모션이 끝나기 전에 다음 공격이 시작돼 연출이 끊긴다.
    public static class AttackSpeed
    {
        // 배수가 클수록 짧아진다. 0 이하(데이터 실수)는 무시하고 원래 시간을 쓴다.
        public static float Scale(float seconds, float speedMult) =>
            speedMult > 0f ? seconds / speedMult : seconds;
    }
}
