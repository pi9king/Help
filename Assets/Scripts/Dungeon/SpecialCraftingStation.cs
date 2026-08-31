using UnityEngine;
using Help.Crafting;
using Help.UI;

namespace Help.Dungeon
{
    // 특수방의 제작대. 플레이어가 범위 안에 있는 동안만 크래프팅 창이 특수 모드가 된다
    // (= E가 들어가지 않은 단어도 조합 가능). 범위를 벗어나면 기본 모드로 돌아간다.
    [RequireComponent(typeof(Collider2D))]
    public class SpecialCraftingStation : MonoBehaviour
    {
        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            SetMode(CraftMode.Special);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            SetMode(CraftMode.Basic);
        }

        // 방을 떠나면 콘텐츠가 비활성화되므로, 특수 모드가 남지 않도록 여기서도 되돌린다.
        private void OnDisable() => SetMode(CraftMode.Basic);

        private static void SetMode(CraftMode mode)
        {
            var ui = FindFirstObjectByType<CraftingBenchUI>();
            if (ui != null) ui.SetCraftMode(mode);
        }
    }
}
