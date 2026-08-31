using System.Collections.Generic;
using Help.Item;

namespace Help.Puzzle
{
    // 능력 타깃(장애물) 해제 판정 — 순수 로직(전투 DamageCalculator 스타일).
    // required == None이면 조건 없음(항상 해제). 아니면 적용된 능력 집합에 required가 있으면 해제.
    // 적용 "출처"는 무관(주무기 공격/서브무기 사용/환경) — 프레임워크 원칙(C 모델).
    public static class CapabilityMatch
    {
        public static bool Resolves(Capability required, IReadOnlyCollection<Capability> applied)
        {
            if (required == Capability.None) return true;
            if (applied == null) return false;
            foreach (var c in applied)
                if (c == required) return true;
            return false;
        }

        // 단일 능력 적용 편의 오버로드
        public static bool Resolves(Capability required, Capability applied)
            => required == Capability.None || applied == required;
    }
}
