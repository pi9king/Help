namespace Help.Combat
{
    // 공격 전달 방식. 타이밍(AttackMotionClock)은 공용이고, 이 종류에 따라
    // 드라이버(PlayerAttack)가 "무엇을 내보내는가"만 달라진다.
    public enum AttackKind
    {
        MeleeArc,    // 근접 슬래시 호 — 앞쪽 히트박스 + 슬래시 VFX
        Projectile,  // 원거리/마법 — 투사체 스폰(추후 구현). 데미지/속성/능력은 그대로 실어 보냄
    }
}
