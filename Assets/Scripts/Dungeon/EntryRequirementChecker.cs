using Help.Combat;
using Help.Crafting;
using Help.Inventory;
using Help.Item;

namespace Help.Dungeon
{
    // 방 진입 판정: 보유 재료 + 장비 분해 재료 풀로 조건에 맞는 아이템을 제작할 수 있는지 확인
    public static class EntryRequirementChecker
    {
        public static bool CanEnter(Room room, Help.Inventory.Inventory inventory, RecipeDatabase database)
            => CanEnter(room, MaterialPoolCalculator.GetTotalPool(inventory), database);

        // 재료 풀을 직접 받는 오버로드 (FloorValidator가 층 재료 풀로 재사용)
        public static bool CanEnter(
            Room room,
            System.Collections.Generic.Dictionary<AlphabetMaterial, int> pool,
            RecipeDatabase database)
        {
            if (room.IsFreeEntry) return true;

            var crafting = new CraftingSystem(database);

            foreach (var condition in room.EntryConditions)
            {
                if (!CanSatisfy(condition, pool, crafting, database)) return false;
            }
            return true;
        }

        private static bool CanSatisfy(
            EntryCondition condition,
            System.Collections.Generic.Dictionary<AlphabetMaterial, int> pool,
            CraftingSystem crafting,
            RecipeDatabase database)
        {
            foreach (var item in database.AllItems)
            {
                if (condition.RequiredElement != ElementType.None && item.Element != condition.RequiredElement) continue;
                if (condition.RequiredWeapon != WeaponCategory.None && item.WeaponCategory != condition.RequiredWeapon) continue;
                if (condition.RequiredCapability != Capability.None &&
                    (item.Capabilities == null || !item.Capabilities.Contains(condition.RequiredCapability))) continue;
                if (crafting.CanCraft(item.Id, pool)) return true;
            }
            return false;
        }
    }
}
