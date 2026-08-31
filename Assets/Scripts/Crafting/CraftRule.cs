using Help.Item;

namespace Help.Crafting
{
    public enum CraftMode
    {
        Basic,   // 일반 크래프팅 창 — E가 들어간 단어만
        Special  // 특수방 제작대 — 기본 규칙 밖(E 없는 단어도 가능)
    }

    // 제작 가능성 판정을 모드별로 한곳에 모은다(순수).
    // 기본 모드는 게임 정체성 규칙(AlphabetWordRule)을 그대로 쓰고,
    // 특수 모드는 그 규칙만 면제한다 — 재료·레시피 요건은 모드와 무관하게 동일하다.
    public static class CraftRule
    {
        public static bool CanCraftWith(ItemDefinition item, CraftMode mode)
        {
            if (item == null || item.Type == ItemType.Material) return false;
            // 레시피가 없으면 재료 요구가 0이라 무한 제작된다 — 모드와 무관하게 금지
            if (item.Recipe == null || item.Recipe.Count == 0) return false;

            return mode == CraftMode.Special || AlphabetWordRule.IsBasicCraftable(item);
        }
    }
}
