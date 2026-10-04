using UnityEngine;
using Help.Item;

namespace Help.Combat
{
    // 공격 한 종류의 타이밍과 판정을 서술한다. ForWeapon에서 무기별 값을 선택한다.
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

        // T1 무기를 하나씩 구현한다. 미구현 무기는 기존 기본 모션을 유지한다.
        public static AttackMotionDef ForWeapon(ItemDefinition weapon)
        {
            if (weapon == null) return Default();

            // x는 조준 방향 길이, y는 측면 너비. 초 단위 타이밍은 총합이 공격 간격이다.
            switch (weapon.Id)
            {
                case "axe": return Melee("AXE", .12f, .18f, .22f, .72f, 1.2f, 2f, 105f, 1.8f);
                case "mace": return Melee("MACE", .15f, .10f, .18f, .62f, 1f, 1.2f, 35f, 1.2f);
                case "pike": return Melee("PIKE", .16f, .09f, .25f, 1.25f, 1.6f, .45f, 8f, 1.1f);
                case "cane": return Melee("CANE", .07f, .11f, .15f, .8f, 1.1f, .8f, 60f, 1.2f);
                case "pipe": return Melee("PIPE", .10f, .10f, .18f, .7f, 1.1f, 1f, 45f, 1.3f);
                case "net": return Melee("NET", .19f, .22f, .19f, .85f, 1.2f, 2.3f, 90f, 2f);
                case "pen": return Melee("PEN", .03f, .08f, .13f, .55f, .6f, .35f, 10f, .8f);
                case "wire": return Melee("WIRE", .16f, .11f, .28f, 1.35f, 1.6f, 1f, 75f, 1.6f);
                case "stone": return Melee("STONE", .15f, .10f, .23f, .5f, .7f, .9f, 30f, 1f);
                case "spade": return Melee("SPADE", .18f, .15f, .22f, .85f, 1.2f, 1.4f, 70f, 1.6f);
                case "epee": return Melee("EPEE", .06f, .07f, .18f, 1.0f, 1.2f, .35f, 8f, 1f);
                case "needle": return Melee("NEEDLE", .02f, .06f, .12f, .45f, .6f, .2f, 5f, .7f);
                case "pestle": return Melee("PESTLE", .11f, .10f, .19f, .55f, .9f, .85f, 25f, 1f);
                case "skewer": return Melee("SKEWER", .10f, .09f, .23f, 1.0f, 1.5f, .5f, 12f, 1.2f);
            }

            return Default();
        }

        private static AttackMotionDef Melee(string name, float windup, float active, float recovery,
            float reach, float length, float width, float halfArc, float scale) => new AttackMotionDef
        {
            Name = name,
            Windup = windup,
            Active = active,
            Recovery = recovery,
            Reach = reach,
            HitboxSize = new Vector2(length, width),
            ArcStartDeg = halfArc,
            ArcEndDeg = -halfArc,
            SlashScale = scale
        };
    }
}
