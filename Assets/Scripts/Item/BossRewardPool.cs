using System.Collections.Generic;

namespace Help.Item
{
    // 순수 로직: 보스 보상 후보(장착 가능한 완성 아이템)에서 층 seed로 결정적 N개를 뽑는다.
    // 재료·소모품은 제외(보스 보상은 바로 쓰는 장비/서브무기/장신구). UnityEngine 비의존 → EditMode 테스트.
    public static class BossRewardPool
    {
        // 장착 가능한 완성 아이템만 후보로 인정(무기/서브무기/방어구/장신구).
        public static bool IsRewardCandidate(ItemType type)
        {
            switch (type)
            {
                case ItemType.Weapon:
                case ItemType.SubWeapon:
                case ItemType.HeadArmor:
                case ItemType.BodyArmor:
                case ItemType.LegArmor:
                case ItemType.Accessory:
                    return true;
                default:
                    return false; // Material, Consumable 제외
            }
        }

        // seed로 결정적 셔플 후 앞 count개 반환(중복 없음). 후보가 count보다 적으면 있는 만큼.
        public static List<ItemDefinition> Pick(IReadOnlyList<ItemDefinition> allItems, int count, int seed)
        {
            var candidates = new List<ItemDefinition>();
            if (allItems != null)
                foreach (var it in allItems)
                    if (it != null && IsRewardCandidate(it.Type)) candidates.Add(it);

            // 결정적 Fisher-Yates (System.Random(seed) — 같은 층=같은 후보)
            var rng = new System.Random(seed);
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            if (count < 0) count = 0;
            if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
            return candidates;
        }
    }
}
