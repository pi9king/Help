using System.Collections.Generic;

namespace Help.Item
{
    // 크래프팅 핵심 정체성 규칙:
    //  - 아이템 단어는 반드시 E를 포함한다 (플레이어 = 알파벳 E).
    //  - 레시피(재료 구성)는 "단어의 글자에서 E 하나를 뺀 것"과 정확히 일치한다.
    //    (E는 플레이어 자신이 조합 시 자동 제공하므로 재료가 아니다.)
    public static class AlphabetWordRule
    {
        public static bool WordContainsE(string word) =>
            !string.IsNullOrEmpty(word) && word.ToUpperInvariant().Contains('E');

        // 기본 제작(발견형 크래프팅)으로 만들 수 있는 아이템인가.
        // 게임 정체성상 "E가 들어간 영단어"만 제작 대상이다(플레이어 = 알파벳 E).
        // 레시피가 비어 있으면 제작 대상이 아니다 — 재료 요구가 0이라 무한 제작되기 때문
        // (포션처럼 드랍으로만 얻는 아이템이 여기 해당).
        public static bool IsBasicCraftable(ItemDefinition item)
        {
            if (item == null) return false;
            if (item.Recipe == null || item.Recipe.Count == 0) return false;
            return WordContainsE(item.Word);
        }

        // 단어에서 E 하나를 제거한 글자 다중집합
        public static Dictionary<char, int> RequiredLetters(string word)
        {
            var counts = new Dictionary<char, int>();
            bool removedOneE = false;
            foreach (char raw in (word ?? string.Empty).ToUpperInvariant())
            {
                if (raw == 'E' && !removedOneE) { removedOneE = true; continue; }
                counts.TryGetValue(raw, out int c);
                counts[raw] = c + 1;
            }
            return counts;
        }

        public static bool RecipeMatchesWord(string word, IEnumerable<MaterialRequirement> recipe)
        {
            if (!WordContainsE(word)) return false;

            var required = RequiredLetters(word);
            var actual = new Dictionary<char, int>();
            foreach (var req in recipe)
            {
                char letter = req.Material.ToString()[0]; // AlphabetMaterial.A → 'A'
                actual.TryGetValue(letter, out int cur);
                actual[letter] = cur + req.Count;
            }

            if (required.Count != actual.Count) return false;
            foreach (var kv in required)
                if (!actual.TryGetValue(kv.Key, out int c) || c != kv.Value) return false;
            return true;
        }
    }
}
