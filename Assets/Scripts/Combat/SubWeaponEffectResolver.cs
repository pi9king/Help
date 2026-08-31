using System.Collections.Generic;
using Help.Enemy;
using Help.Item;

namespace Help.Combat
{
    // 서브무기 "사용(use)"이 적에게 남기는 전투 효과.
    public struct SubWeaponEffect
    {
        public StatusEffectType Status;
        public float Duration;
        public float PullDuration; // > 0이면 적용 직후 이 시간만큼 시전자 쪽으로 끌려온다

        public bool HasEffect => Status != StatusEffectType.None;
    }

    // 능력(Capability) → 전투 효과 매핑(순수 static — CapabilityMatch의 전투판 미러).
    // 퍼즐 장애물은 CapabilityMatch가, 적은 여기가 담당한다. 둘 다 "적용된 능력 집합"만 본다(출처 무관).
    // 확장: 새 전투 능력은 아래 switch에 case 하나 추가.
    public static class SubWeaponEffectResolver
    {
        public const float BindDuration = 2f;
        public const float BindPullDuration = 0.4f; // 로프: 묶은 뒤 짧게 끌어당긴다

        public static SubWeaponEffect Resolve(IReadOnlyCollection<Capability> applied)
        {
            if (applied == null) return default;

            foreach (var cap in applied)
            {
                switch (cap)
                {
                    case Capability.Bind:
                        return new SubWeaponEffect
                        {
                            Status = StatusEffectType.Bind,
                            Duration = BindDuration,
                            PullDuration = BindPullDuration,
                        };
                }
            }
            return default; // 나머지 능력(BreakWall/Melt 등)은 퍼즐 전용 — 적에겐 효과 없음
        }
    }
}
