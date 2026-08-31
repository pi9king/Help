using System;
using System.Collections.Generic;
using Help.Item;

namespace Help.Enemy
{
    public struct DropResult
    {
        public AlphabetMaterial Material;
        public int Count;
    }

    // 순수 로직: 드랍 테이블을 확률 판정해 실제로 나오는 재료 목록을 만든다.
    // rng()는 [0,1) 난수. UnityEngine 비의존 → EditMode 테스트 가능.
    public static class DropRoller
    {
        public static List<DropResult> Roll(IReadOnlyList<DropEntry> entries, Func<float> rng)
        {
            var result = new List<DropResult>();
            if (entries == null) return result;

            foreach (var e in entries)
            {
                if (e == null || e.Count <= 0) continue;
                float chance = e.Chance;
                if (chance <= 0f) continue;
                if (chance >= 1f || (rng != null && rng() < chance))
                    result.Add(new DropResult { Material = e.Material, Count = e.Count });
            }
            return result;
        }
    }
}
