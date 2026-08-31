using System;
using System.Collections.Generic;
using Help.Item;

namespace Help.Inventory
{
    public class Inventory
    {
        private readonly List<ItemStack> _items = new();
        private readonly Dictionary<EquipmentSlotType, ItemStack> _equipped = new();

        public IReadOnlyList<ItemStack> Items => _items;
        public IReadOnlyDictionary<EquipmentSlotType, ItemStack> Equipped => _equipped;

        public event Action OnChanged;
        public event Action<ItemDefinition> OnItemEquipped;
        public event Action<ItemDefinition> OnItemUnequipped;

        public void Add(ItemDefinition item, int count = 1)
        {
            var stack = _items.Find(s => s.Definition.Id == item.Id);
            if (stack != null)
                stack.Add(count);
            else
                _items.Add(new ItemStack(item, count));
            OnChanged?.Invoke();
        }

        public bool Remove(ItemDefinition item, int count = 1)
        {
            var stack = _items.Find(s => s.Definition.Id == item.Id);
            if (stack == null || stack.Count < count) return false;
            stack.Remove(count);
            if (stack.Count == 0) _items.Remove(stack);
            OnChanged?.Invoke();
            return true;
        }

        public int CountOf(ItemDefinition item)
        {
            var stack = _items.Find(s => s.Definition.Id == item.Id);
            return stack?.Count ?? 0;
        }

        // 인벤토리 전체 비우기(런 리셋용). 장착품은 Unequip으로 되돌려 구독자(스탯 보너스)가 정상 해제되게 한다.
        public void Clear()
        {
            foreach (var slot in new List<EquipmentSlotType>(_equipped.Keys))
                Unequip(slot); // OnItemUnequipped 발생 → 장비 보너스 해제
            _items.Clear();
            OnChanged?.Invoke();
        }

        public bool Equip(ItemStack stack, EquipmentSlotType slot)
        {
            if (!_items.Contains(stack)) return false;
            // 아이템 유형에 맞지 않는 슬롯이면 거부 (재료/소모품 장착 방지)
            if (!EquipmentSlotResolver.TryResolve(stack.Definition.Type, out var required) || required != slot)
                return false;
            if (_equipped.ContainsKey(slot)) Unequip(slot);

            // 스택에서 한 개만 분리해 장착 (나머지는 인벤토리에 유지)
            if (stack.Count > 1)
            {
                stack.Remove(1);
                _equipped[slot] = new ItemStack(stack.Definition, 1);
            }
            else
            {
                _items.Remove(stack);
                _equipped[slot] = stack;
            }
            OnItemEquipped?.Invoke(stack.Definition);
            OnChanged?.Invoke();
            return true;
        }

        public ItemStack Unequip(EquipmentSlotType slot)
        {
            if (!_equipped.TryGetValue(slot, out var stack)) return null;
            _equipped.Remove(slot);

            // 같은 Id 스택이 있으면 병합, 없으면 추가 (중복 스택 방지)
            var existing = _items.Find(s => s.Definition.Id == stack.Definition.Id);
            if (existing != null) existing.Add(stack.Count);
            else _items.Add(stack);

            OnItemUnequipped?.Invoke(stack.Definition);
            OnChanged?.Invoke();
            return stack;
        }

        public Dictionary<AlphabetMaterial, int> GetRawMaterials()
        {
            var result = new Dictionary<AlphabetMaterial, int>();
            foreach (var stack in _items)
            {
                if (stack.Definition.Type != ItemType.Material) continue;
                foreach (var req in stack.Definition.Recipe)
                    AddMaterial(result, req.Material, req.Count * stack.Count);
            }
            return result;
        }

        private static void AddMaterial(Dictionary<AlphabetMaterial, int> dict, AlphabetMaterial mat, int count)
        {
            dict.TryGetValue(mat, out int current);
            dict[mat] = current + count;
        }
    }
}
