using System.Collections.Generic;
using Help.Item;

namespace Help.Crafting
{
    // 한 재료의 충족 상태 (크래프팅 UI 표시용). "R×2 (보유 1, 부족 1)" 같은 항목을 그리기 위한 값.
    public readonly struct MaterialStatus
    {
        public readonly AlphabetMaterial Material;
        public readonly int Required;
        public readonly int Have;

        public MaterialStatus(AlphabetMaterial material, int required, int have)
        {
            Material = material;
            Required = required;
            Have = have;
        }

        public int Missing => Required > Have ? Required - Have : 0;
        public bool Satisfied => Have >= Required;
    }

    // 레시피 + 재료 풀 → 재료별 필요/보유 내역. 순수 로직(EditMode 테스트 가능).
    // CraftingSystem.CanCraft가 true/false만 주는 것과 달리, UI가 "무엇이 얼마나 부족한지"를 보여줄 수 있게 한다.
    public static class RecipeAvailability
    {
        public static List<MaterialStatus> Evaluate(
            IEnumerable<MaterialRequirement> recipe,
            IReadOnlyDictionary<AlphabetMaterial, int> pool)
        {
            // 같은 재료가 레시피에 여러 번 들어오면 합산한다 (예: HAMMER의 M×2).
            var required = new Dictionary<AlphabetMaterial, int>();
            foreach (var req in recipe)
            {
                required.TryGetValue(req.Material, out int current);
                required[req.Material] = current + req.Count;
            }

            var result = new List<MaterialStatus>(required.Count);
            foreach (var pair in required)
            {
                int have = 0;
                pool?.TryGetValue(pair.Key, out have);
                result.Add(new MaterialStatus(pair.Key, pair.Value, have));
            }

            // enum 값 오름차순으로 안정 정렬 → UI 표시 순서가 매 갱신마다 흔들리지 않는다.
            result.Sort((a, b) => a.Material.CompareTo(b.Material));
            return result;
        }

        public static bool CanCraft(
            IEnumerable<MaterialRequirement> recipe,
            IReadOnlyDictionary<AlphabetMaterial, int> pool)
        {
            foreach (var status in Evaluate(recipe, pool))
                if (!status.Satisfied) return false;
            return true;
        }
    }
}
