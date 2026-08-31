using System.Collections.Generic;
using Help.Item;

namespace Help.Inventory
{
    // 보유 재료 + 모든 장비 분해 시 나오는 재료를 합산
    public static class MaterialPoolCalculator
    {
        public static Dictionary<AlphabetMaterial, int> GetTotalPool(Inventory inventory)
        {
            var pool = new Dictionary<AlphabetMaterial, int>();

            // 인벤토리 내 재료 아이템
            foreach (var stack in inventory.Items)
            {
                if (stack.Definition.Type != ItemType.Material) continue;
                foreach (var req in stack.Definition.Recipe)
                    Add(pool, req.Material, req.Count * stack.Count);
            }

            // 인벤토리 내 장비 분해 시 재료
            foreach (var stack in inventory.Items)
            {
                if (stack.Definition.Type == ItemType.Material) continue;
                AddFromRecipe(pool, stack);
            }

            // 장착 중인 장비 분해 시 재료
            foreach (var kv in inventory.Equipped)
                AddFromRecipe(pool, kv.Value);

            return pool;
        }

        private static void AddFromRecipe(Dictionary<AlphabetMaterial, int> pool, ItemStack stack)
        {
            foreach (var req in stack.Definition.Recipe)
                Add(pool, req.Material, req.Count * stack.Count);
        }

        private static void Add(Dictionary<AlphabetMaterial, int> dict, AlphabetMaterial mat, int count)
        {
            dict.TryGetValue(mat, out int cur);
            dict[mat] = cur + count;
        }
    }
}
