using System.Collections.Generic;
using Help.Item;

namespace Help.Crafting
{
    // 크래프팅 슬롯 상태(순수). 각 슬롯에 글자(재료)를 배치/제거하고, 현재 배치가 어떤 아이템을 만드는지 질의한다.
    // UI(MonoBehaviour)는 이 상태를 그리기만 하고, 판정은 RecipeMatcher에 위임한다(로직/뷰 분리 원칙).
    public class CraftingBench
    {
        private readonly AlphabetMaterial?[] _slots;

        public CraftingBench(int slotCount)
        {
            if (slotCount < 1) slotCount = 1;
            _slots = new AlphabetMaterial?[slotCount];
        }

        public int SlotCount => _slots.Length;

        public AlphabetMaterial? GetSlot(int index) =>
            InRange(index) ? _slots[index] : null;

        // 슬롯에 글자 배치(기존 값은 덮어씀). 범위를 벗어나면 false.
        public bool Place(int index, AlphabetMaterial material)
        {
            if (!InRange(index)) return false;
            _slots[index] = material;
            return true;
        }

        // 슬롯 비우기. 원래 글자가 있었으면 true.
        public bool Remove(int index)
        {
            if (!InRange(index) || !_slots[index].HasValue) return false;
            _slots[index] = null;
            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = null;
        }

        public bool IsEmpty
        {
            get
            {
                foreach (var s in _slots)
                    if (s.HasValue) return false;
                return true;
            }
        }

        // 배치된 글자들(빈 슬롯 제외).
        public List<AlphabetMaterial> PlacedMaterials()
        {
            var result = new List<AlphabetMaterial>();
            foreach (var s in _slots)
                if (s.HasValue) result.Add(s.Value);
            return result;
        }

        // 현재 배치가 만들어내는 아이템(정확 일치). 없으면 null → UI가 제작 버튼을 비활성.
        // mode=Special이면 특수방 제작대 — E 없는 단어도 매칭된다.
        public ItemDefinition Result(IReadOnlyList<ItemDefinition> items, CraftMode mode = CraftMode.Basic) =>
            RecipeMatcher.FindExact(PlacedMaterials(), items, mode);

        private bool InRange(int index) => index >= 0 && index < _slots.Length;
    }
}
