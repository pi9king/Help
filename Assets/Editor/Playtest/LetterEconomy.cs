using System.Collections.Generic;
using System.Linq;
using Help.Dungeon;
using Help.Item;

namespace Help.Editor.Playtest
{
    public sealed class LetterEconomyStats
    {
        public int BudgetSize;
        public List<ItemDefinition> ExtraCraftable = new();        // 층 레시피 외에 예산만으로 만들 수 있는 것
    }

    // 층 예산의 "선택 압력"을 잰다 — 체크리스트 P1-7(글자 예산에 진짜 기회비용 만들기)의 측정 쪽.
    // 예산 계산 자체는 FloorLootPlan.BuildLetterBudget을 그대로 쓴다(규칙을 복제하지 않는다).
    public static class LetterEconomy
    {
        public static LetterEconomyStats Analyze(IReadOnlyList<ItemDefinition> floorRecipes,
                                                 IEnumerable<ItemDefinition> candidates)
        {
            var budget = Count(FloorLootPlan.BuildLetterBudget(floorRecipes));
            var stats = new LetterEconomyStats { BudgetSize = budget.Values.Sum() };

            // "두 레시피가 같은 글자를 쓴다"는 기회비용이 아니다 — 예산이 레시피 글자의 합이라
            // 그 글자가 두 장 들어 있다. 기회비용은 예산으로 **다른 것**을 만들 수 있을 때만 생긴다.

            foreach (var item in candidates)
            {
                if (item == null || floorRecipes.Contains(item)) continue;
                var need = Count(FloorLootPlan.BuildLetterBudget(new[] { item }));
                if (need.Count == 0) continue;
                if (need.All(kv => budget.TryGetValue(kv.Key, out var have) && have >= kv.Value))
                    stats.ExtraCraftable.Add(item);
            }
            return stats;
        }

        private static Dictionary<AlphabetMaterial, int> Count(IEnumerable<AlphabetMaterial> letters)
        {
            var d = new Dictionary<AlphabetMaterial, int>();
            foreach (var l in letters) d[l] = d.TryGetValue(l, out var n) ? n + 1 : 1;
            return d;
        }
    }
}
