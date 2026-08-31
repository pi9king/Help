using System.Collections.Generic;
using Help.Crafting;
using Help.Item;

namespace Help.Dungeon
{
    // 층 드랍 테이블(예산) 계산 — 순수 로직.
    //
    // 설계 의도: 층에 뿌리는 알파벳 총량을 "아이템 N개를 만들 수 있는 양"으로 못박는다.
    // 글자가 무한정 쏟아지면 "무엇을 만들지" 고르는 재미가 사라지기 때문이다.
    // 플레이어는 이 예산 안에서 선택한다 — 지금 강해지려고 글자를 써버릴 것인가,
    // 층의 모든 알파벳을 살릴 것인가.
    public static class FloorLootPlan
    {
        // 층에서 만들 수 있는 아이템을 고른다.
        // 진입 열쇠(keyItems)는 클리어 가능성이 걸려 있으므로 무조건 포함하고(count를 넘더라도),
        // 남는 자리는 기본 제작 가능한 후보에서 결정적으로 채운다.
        public static List<ItemDefinition> SelectRecipes(
            RecipeDatabase database, IReadOnlyList<ItemDefinition> keyItems, int count, int seed)
        {
            var picked = new List<ItemDefinition>();
            if (keyItems != null)
                foreach (var key in keyItems)
                    if (key != null && !picked.Contains(key)) picked.Add(key);

            if (database == null) return picked;

            var candidates = new List<ItemDefinition>();
            foreach (var item in database.AllItems)
                if (AlphabetWordRule.IsBasicCraftable(item) && !picked.Contains(item))
                    candidates.Add(item);

            // 결정적 셔플(Fisher-Yates) — 같은 시드는 같은 층 테이블 (BossRewardPool과 같은 방식)
            var rng = new System.Random(seed);
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }

            foreach (var candidate in candidates)
            {
                if (picked.Count >= count) break;
                picked.Add(candidate);
            }
            return picked;
        }

        // 선택된 아이템들의 레시피를 글자 단위로 평탄화 = 층 전체 알파벳 총량.
        public static List<AlphabetMaterial> BuildLetterBudget(IEnumerable<ItemDefinition> items)
        {
            var letters = new List<AlphabetMaterial>();
            if (items == null) return letters;

            foreach (var item in items)
            {
                if (item == null || item.Recipe == null) continue;
                foreach (var req in item.Recipe)
                    for (int c = 0; c < req.Count; c++)
                        letters.Add(req.Material);
            }
            return letters;
        }
    }
}
