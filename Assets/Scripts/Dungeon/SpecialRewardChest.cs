using System.Collections.Generic;
using UnityEngine;
using Help.Core;
using Help.Crafting;
using Help.Item;

namespace Help.Dungeon
{
    // 특수방의 보상 상자. 닿으면 "기본 제작으로는 만들 수 없는 아이템"(E 없는 단어) 하나를 준다.
    // 특수방 자체가 확률 등장이므로 이 보상은 클리어에 필요한 것이 아니라 순수 행운이다.
    [RequireComponent(typeof(Collider2D))]
    public class SpecialRewardChest : MonoBehaviour
    {
        [SerializeField] private bool _opened;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_opened || !other.CompareTag("Player")) return;

            var gm = GameManager.Instance;
            if (gm == null || gm.RecipeDatabase == null) return;

            var reward = PickReward(gm.RecipeDatabase);
            if (reward == null) return; // 특수 아이템이 없으면 상자를 열지 않고 남겨둔다

            gm.Inventory.Add(reward, 1);
            _opened = true;
            gameObject.SetActive(false); // 방 캐시에 남아 재방문 시 다시 열리지 않는다
        }

        // 기본 제작 불가(= 특수) 아이템 중 하나. 방 위치로 결정적 선택 — 재방문해도 같은 물건.
        private ItemDefinition PickReward(RecipeDatabase database)
        {
            var candidates = new List<ItemDefinition>();
            foreach (var item in database.AllItems)
                if (!AlphabetWordRule.IsBasicCraftable(item) && CraftRule.CanCraftWith(item, CraftMode.Special))
                    candidates.Add(item);

            if (candidates.Count == 0) return null;

            int seed = Mathf.RoundToInt(transform.position.x * 31f + transform.position.y * 17f);
            return candidates[Mathf.Abs(seed) % candidates.Count];
        }
    }
}
