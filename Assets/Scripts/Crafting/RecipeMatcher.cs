using System.Collections.Generic;
using Help.Item;

namespace Help.Crafting
{
    // 배치된 재료(글자) 묶음 → 정확히 일치하는 레시피의 아이템을 찾는다.
    // 발견형 크래프팅의 두뇌: 레시피 목록을 보여주지 않고, "슬롯에 놓은 글자 조합이 유효한 단어면 활성화"한다.
    // 정확 일치 = 배치한 글자 종류·개수가 레시피와 완전히 같아야 함(남는 글자가 있으면 불일치). 순수 로직.
    public static class RecipeMatcher
    {
        public static ItemDefinition FindExact(
            IEnumerable<AlphabetMaterial> placed,
            IReadOnlyList<ItemDefinition> items,
            CraftMode mode = CraftMode.Basic)
        {
            var placedCounts = Tally(placed);
            if (placedCounts.Count == 0 || items == null) return null;

            foreach (var item in items)
            {
                if (item == null || item.Recipe == null) continue;
                // 재료 자기참조 레시피(mat_K = K×1 등)는 제외 — 글자 하나 놓았다고 재료가 "제작"되면 안 된다.
                // 기본 모드는 E가 들어간 단어만, 특수방 제작대(Special)는 그 규칙을 면제받는다.
                if (!CraftRule.CanCraftWith(item, mode)) continue;

                if (Equal(placedCounts, TallyRecipe(item.Recipe)))
                    return item;
            }
            return null;
        }

        private static Dictionary<AlphabetMaterial, int> Tally(IEnumerable<AlphabetMaterial> mats)
        {
            var counts = new Dictionary<AlphabetMaterial, int>();
            if (mats == null) return counts;
            foreach (var m in mats)
            {
                counts.TryGetValue(m, out int c);
                counts[m] = c + 1;
            }
            return counts;
        }

        private static Dictionary<AlphabetMaterial, int> TallyRecipe(List<MaterialRequirement> recipe)
        {
            var counts = new Dictionary<AlphabetMaterial, int>();
            foreach (var req in recipe)
            {
                counts.TryGetValue(req.Material, out int c);
                counts[req.Material] = c + req.Count;
            }
            return counts;
        }

        private static bool Equal(Dictionary<AlphabetMaterial, int> a, Dictionary<AlphabetMaterial, int> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var pair in a)
            {
                if (!b.TryGetValue(pair.Key, out int bv) || bv != pair.Value) return false;
            }
            return true;
        }
    }
}
