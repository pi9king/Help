using UnityEngine;

namespace Help.Combat
{
    // 공격 한 종류를 서술하는 데이터. 무기별로 이 값만 달리하면 모션이 갈린다(코드 불변).
    // 지금은 코드 기본값 1종을 쓰고, 추후 WeaponCategory→AttackMotionDef 라이브러리(ScriptableObject)로 확장.
    [System.Serializable]
    public class AttackMotionDef
    {
        public string Name = "Default";
        public AttackKind Kind = AttackKind.MeleeArc;

        // 타이밍(초)
        public float Windup = 0.03f;
        public float Active = 0.12f;
        public float Recovery = 0.10f;

        // 근접(MeleeArc)
        public float Reach = 0.7f;                       // 히트박스 전방 거리(사거리)
        public Vector2 HitboxSize = new Vector2(1.1f, 1.3f); // 히트박스 크기(범위)
        public float ArcStartDeg = 75f;                  // 슬래시 스윙 시작 각(위)
        public float ArcEndDeg = -75f;                   // 끝 각(아래)
        public Color SlashColor = new Color(1f, 1f, 1f, 0.9f);
        public float SlashScale = 1.5f;

        // 원거리/마법(Projectile) — 추후 사용
        public GameObject ProjectilePrefab;
        public float ProjectileSpeed = 12f;

        public float TotalDuration => Windup + Active + Recovery;

        // 맨손/미등록 무기용 기본 근접 모션
        public static AttackMotionDef Default() => new AttackMotionDef();
    }
}
