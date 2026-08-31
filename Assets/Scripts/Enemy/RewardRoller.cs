using System;
using System.Collections.Generic;
using Help.Item;

namespace Help.Enemy
{
    public struct RewardResult
    {
        public RewardKind Kind;
        public int Amount;
    }

    // 순수 로직: 보상 테이블을 확률 판정해 실제로 나오는 재화 목록을 만든다(DropRoller의 재화판).
    // rng()는 [0,1) 난수. UnityEngine 비의존 → EditMode 테스트 가능.
    public static class RewardRoller
    {
        public static List<RewardResult> Roll(IReadOnlyList<RewardEntry> entries, Func<float> rng)
        {
            var result = new List<RewardResult>();
            if (entries == null) return result;

            foreach (var e in entries)
            {
                if (e == null || e.Amount <= 0) continue;
                float chance = e.Chance;
                if (chance <= 0f) continue;
                if (chance >= 1f || (rng != null && rng() < chance))
                    result.Add(new RewardResult { Kind = e.Kind, Amount = e.Amount });
            }
            return result;
        }
    }
}
