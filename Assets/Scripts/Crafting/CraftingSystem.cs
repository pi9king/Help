using System.Collections.Generic;
using Help.Item;
using Help.Inventory;

namespace Help.Crafting
{
    public class CraftingSystem
    {
        private readonly RecipeDatabase _db;

        public CraftingSystem(RecipeDatabase db) => _db = db;

        public bool CanCraft(string itemId, Dictionary<AlphabetMaterial, int> availableMaterials,
            CraftMode mode = CraftMode.Basic)
        {
            var item = _db.Find(itemId);
            if (!CraftRule.CanCraftWith(item, mode)) return false;
            return HasEnoughMaterials(item, availableMaterials);
        }

        // 인벤토리에서 재료를 소모하고 아이템을 추가
        public bool Craft(string itemId, Help.Inventory.Inventory inventory,
            CraftMode mode = CraftMode.Basic)
        {
            var item = _db.Find(itemId);
            // 판정(CanCraft)과 같은 기준을 쓴다 — 기본 제작 대상은 E가 들어간 단어뿐,
            // 특수방 제작대(Special)만 그 규칙을 면제받는다
            if (!CraftRule.CanCraftWith(item, mode)) return false;

            var pool = inventory.GetRawMaterials();
            if (!HasEnoughMaterials(item, pool)) return false;

            foreach (var req in item.Recipe)
                RemoveMaterial(inventory, req.Material, req.Count);

            inventory.Add(item);
            return true;
        }

        // 아이템을 분해하여 재료 반환
        public bool Disassemble(string itemId, Help.Inventory.Inventory inventory)
        {
            var item = _db.Find(itemId);
            if (item == null) return false;
            if (!inventory.Remove(item)) return false;

            foreach (var req in item.Recipe)
            {
                var matItem = _db.FindMaterial(req.Material);
                if (matItem != null) inventory.Add(matItem, req.Count);
            }
            return true;
        }

        private bool HasEnoughMaterials(ItemDefinition item, Dictionary<AlphabetMaterial, int> pool)
        {
            foreach (var req in item.Recipe)
            {
                pool.TryGetValue(req.Material, out int have);
                if (have < req.Count) return false;
            }
            return true;
        }

        private void RemoveMaterial(Help.Inventory.Inventory inventory, AlphabetMaterial mat, int count)
        {
            var matItem = _db.FindMaterial(mat);
            if (matItem != null) inventory.Remove(matItem, count);
        }
    }
}
