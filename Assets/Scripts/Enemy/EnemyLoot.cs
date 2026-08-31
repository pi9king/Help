using System.Collections.Generic;
using Help.Combat;
using Help.Item;

namespace Help.Enemy
{
    // 순수 로직: 적의 잠금 속성 단어에서 드롭 재료(글자)를 뽑는다.
    // 알파벳 테마 — 속성 단어의 각 글자가 재료이며, E는 플레이어 자신이라 제외한다.
    // 예: Fire→F,I,R / Steel→S,T,L / Ether→T,H,R / None→(없음).
    // 속성 단어의 글자가 그대로 재료가 되어 크래프팅 재료 풀에 더해진다(특정 무기 레시피와 1:1은 아님).
    public static class EnemyLoot
    {
        public static IReadOnlyList<AlphabetMaterial> ForElement(ElementType element)
        {
            var result = new List<AlphabetMaterial>();
            if (element == ElementType.None) return result;

            foreach (char c in element.ToString().ToUpperInvariant())
            {
                if (c == 'E') continue; // E 제외(플레이어 자신)
                if (System.Enum.TryParse<AlphabetMaterial>(c.ToString(), out var mat))
                    result.Add(mat);
            }
            return result;
        }
    }
}
